## VBR Reports
This is a project I created to have daily reporting on machines backed up by Veeam Backup and Replication Servers.

### Description
This addresses various issues in VSC and other reporting tools such as:

- When a VM may no longer be processed by a backup job but be kept in VBR as a temporary archive,
yet VSC will alert on them. Often times this results in technicians wasting their time investigating an issue 
that doesn't exist.
- Handles duplicate VM names across client sites.
- Easy to use export to csv function, for daily reporting needs.
- Handles deletion of VMs from VBR jobs (Will no longer provide reporting once they are deleted)

### Technologies Used

- The Ingestion service is written in ASP.NET Core (.NET Minimal API)
- SQLite
- Front end is written in react, served by the ingestion service.

### How it works

- On each VBR server, a PowerShell Script gathers data on the latest restore points and sends it to the ingestion service
- The ingestion service processes the data, and stores it in a SQLite database.
- The ingestion service serves the static HTML files, where the data is displayed and can be exported