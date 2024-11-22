$latestRestorePointsData = @()

# Get all backup jobs
$jobs = Get-VBRJob

# Hostname of current VBR machine
$vbrName = $env:ComputerName

# If we can't find name, try again with WMI
if ($vbrName.Length -le 2) {
    $vbrName = (Get-WmiObject Win32_Computersystem).Name
}

# get the associated backup and latest restore point for each VM
foreach ($job in $jobs) {
    $backup = Get-VBRBackup | Where-Object { $_.JobId -eq $job.Id }

    # Check if the backup exists (some jobs might not have associated backups)
    if ($backup) {
        # Retrieve the list of VMs currently included in this job
        $jobVms = $job.GetObjectsInJob() | Where-Object { $_.Type -eq 'Include' } | Select-Object -ExpandProperty Name

        # Filter to include only VMs still in the job
        $latestRestorePoints = Get-VBRRestorePoint -Backup $backup |
            Where-Object { $jobVms -contains $_.Name } |
            Sort-Object -Property CreationTime -Descending |
            Group-Object -Property VMName | ForEach-Object {
                $_.Group | Select-Object -First 1
            }

        # Collect each latest restore point into our array
        foreach ($restorePoint in $latestRestorePoints) {
            # Create a custom object for each restore point with the required properties
            $restorePointInfo = [PSCustomObject]@{
                VbrHost    = $vbrName
                ParentJob  = $job.Name
                HostName   = $restorePoint.Name
                Date       = $restorePoint.CreationTime.ToString("yyyy-MM-dd")
            }
            
            # Add the custom object to the array
            $latestRestorePointsData += $restorePointInfo
        }
    }
}

$jsonOutput = $latestRestorePointsData | ConvertTo-Json -Depth 3

# URL for the POST request
$url = ""
Invoke-WebRequest -Uri $url -Method POST -Body $jsonOutput -ContentType 'application/json'
