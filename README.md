# 🚀 Job Management App

## 📋 About
The Job Management App is a full-stack application designed to manage and monitor job execution in a distributed system. It features a real-time dashboard that displays job status, progress, and queue information.  
The application leverages SignalR for real-time communication between the server, worker services, and the client application.  
Currently whe work time of the jobs are mocked and randomly generated from 1 second to 10 minutes.  

## 🔄 System Architecture
![JobManagmentSystem drawio](https://github.com/user-attachments/assets/ac85eb4e-c2b9-4507-b1c0-5bd0e9accb12)

*System architecture diagram showing the flow of data between components*

## ✨ Key Features
- ⚡ Real-time job monitoring with live updates of job progress and status via SignalR
- 🌐 Distributed job processing architecture via worker nodes
- 📊 Detailed job status tracking and error handling
- ⚕️ Backup & Recovery workflow for queued/runing jobs
- 🗒️ Dynamic settings that can be easily changed
- 🖥️ Interactive dashboard for job management
- 🔄 Priority-based job queuing system using priority queue ordering: 1.Priority 2.Queuing time
- ⚖️ Horizontal scaling of worker nodes to handle large overhead and ensure optimal performance under heavy load.

## 💻 Tech Stack

### Backend
- .NET 8
- SignalR Hub for real-time communication
- SQL Server for data persistence
- Workers-Service with PriorityQueue
- RESTful API with Controllers

### Frontend
- React with TypeScript
- React Context 
- Vite for build tooling
- SignalR Client for real-time updates
- Vanilla CSS for styling
- Responsive & minimal

## 🏗️ Project Structure

### Frontend Application
The frontend application is a React TypeScript application built with Vite. It provides an intuitive interface for monitoring and managing jobs.

#### Key Components
- **Dashboard**: Shows an overview of all jobs and their statuses
- **Job List**: Displays detailed information about each job including Name, Priority, Status, Start Time, End Time, and Progress
- **Job Creation**: Popup form to create new jobs with Name and Priority (High & Regular)
- **Job Management**: Interface to stop running jobs, restart failed jobs, and delete completed/failed jobs
- **SignalR Client**: Establishes real-time connection with the backend for live updates

### Backend Services - C# .NET

#### MainServer
The MainServer is a .NET 8 API application that serves as the central hub for the job management system.

##### Key Components
- **SignalRHub**: Handles real-time communication and acts as a hub for the front application and the worker-service clients
- **Controllers**: Provide REST API endpoints for job management
- **Database Context**: Handles data persistence using Entity Framework Core

#### JobsWorkerService
The JobsWorkerService is responsible for processing jobs using a priority queue and distributing them to worker nodes while scaling them up/down.

##### Key Components
- **JobQueue**: Manages jobs based on priority, where jobs with higher priority are executed first. Queuing time is used as a secondary filter to determine execution order for jobs with the same priority.
- **WorkerNodes**: The micro proceessors of the job queue workflow, handling incoming jobs from the queue.
- **SignalR Client**: Communicates with the MainServer for real-time updates.
- **JobQueueManager**: The unit which controls the assignment and scalling of the WorkerNodes.

## Backend Setup

### Prerequisites:
- **.NET 8 SDK**: Ensure you have the .NET 8 SDK installed. You can download it from the [official .NET download page](https://dotnet.microsoft.com/download/dotnet).
- **SQL Server**: You should have SQL Server set up and accessible.
- **SQL Server Database**: Set up the database using the SQL queries below.

### Database Schema

Run the following SQL queries in **SQL Server Management Studio (SSMS)** to create the necessary tables:

#### Jobs Table:
```sql
CREATE TABLE [JobSystemDB].[dbo].[Jobs] (
    [JobID] UNIQUEIDENTIFIER PRIMARY KEY,  -- Primary key, JobID
    [Name] NVARCHAR(255) NOT NULL,  -- Job name, cannot be NULL
    [Status] INT NOT NULL,  -- Enum for Job Status (0: Pending, 1: InProgress, 2: Completed, etc.)
    [Priority] INT NOT NULL,  -- Enum for Job Priority (0: Low, 1: High, etc.)
    [CreatedAt] BIGINT NOT NULL,  -- Timestamp of when the job was created
    [StartedAt] BIGINT NOT NULL,  -- Timestamp of when the job started
    [CompletedAt] BIGINT NOT NULL,  -- Timestamp of when the job completed
    [Progress] INT CHECK (Progress BETWEEN 0 AND 100),  -- Job progress (0 to 100%)
    [ErrorMessage] NVARCHAR(1000) NULL  -- Optional error message, can be NULL
);
```

#### QueueBackupJobs Table:
```sql
CREATE TABLE [JobSystemDB].[dbo].[QueueBackupJobs] (
    [JobID] UNIQUEIDENTIFIER PRIMARY KEY,  -- Primary key, JobID
    [Priority] INT NOT NULL,  -- Job priority (Enum as integer)
    [QueuingTime] BIGINT NOT NULL,  -- Time the job was queued
    [BackupTimestamp] BIGINT NOT NULL,  -- Timestamp of when the backup was made
    CONSTRAINT FK_QueueBackupJobs_Jobs FOREIGN KEY ([JobID]) 
        REFERENCES [JobSystemDB].[dbo].[Jobs]([JobID])  -- Foreign key to Jobs table
);
```

### 1. Clone the Repository
If you haven't cloned the repository yet, do so by running:

```bash
git clone https://github.com/nqoy/Job-Management-App.git
cd Job-Management-App
```

### 2. Configure appsettings.json
appsettings.json files in both the MainServer and JobsWorkerService projects are configured with the SignalR and SQL Server deafult connection settings.

Example appsettings.json configuration:

```json
{
  "SignalR": {
    "BaseUrl": "https://localhost:5000",
    "HubPath": "/JobSignalRHub"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=JobSystemDB;Trusted_Connection=True;"
  }
}
```

### 3. Run the Backend (API Server and Worker Service) Without an IDE

#### Running the Backend with .NET CLI:

1. **Restore Dependencies**
   In the terminal, run the following command to restore all dependencies:

   ```bash
   dotnet restore
   ```

2. **Build the Solution**
   Next, build the solution using the command:

   ```bash
   dotnet build
   ```

3. **Run the MainServer Project**
   After building, navigate to the MainServer folder and run:

   ```bash
   dotnet run --project MainServer/MainServer.csproj
   ```
   This will start the API server and SignalR hub. The server will be accessible at https://localhost:5001.

4. **Run the JobsWorkerService Project**
   In a new terminal window, navigate to the JobsWorkerService folder and run:

   ```bash
   dotnet run --project JobsWorkerService.csproj
   ```
   This starts the worker service that handles job processing and worker scaling.

5. **Verify the Backend is Running**
   After running both projects, the backend should be up and running. You can test the API using Postman or a browser, and SignalR will provide real-time job status updates.

### 4. Logging Options for Backend

The backend projects (MainServer and JobsWorkerService) provide two logging configurations:

- **Normal Logging Console**: This provides basic logging output to the console.
- **Debug Console**: This provides more detailed logging, useful for debugging and troubleshooting.

#### How to Run with Different Logging Options

When running the backend projects using Visual Studio or Visual Studio Code, you can select the desired launch profile to control the level of logging. Here's how to configure and run each profile:

- Normal Logging Console:
  - In your launch settings (Properties/launchSettings.json), use the NormalLoggingConsole profile for standard logging.
- Debug Console:
  - If you want more detailed logs, choose the DebugConsole profile for more verbose output, which includes detailed steps and more.

To run a specific profile from the command line, use the following:

```bash
dotnet run --project MainServer/MainServer.csproj --launch-profile <SERVER_NAME>
```

Or for DebugConsole:

```bash
dotnet run --project MainServer/MainServer.csproj --launch-profile DebugConsole
```
![image](https://github.com/user-attachments/assets/05876ba2-d4ac-47c1-b95d-7364be613e4a)




## Frontend Setup ⚛️

![image](https://github.com/user-attachments/assets/bab58ff6-aa74-4c75-96e3-07428b276deb)



1. **Navigate to the Frontend Directory**
2. **Install Dependencies**: Install the required Node.js dependencies:

   ```bash
   npm install
   ```

3. **Run the Frontend**: Start the frontend app using:

   ```bash
   npm run dev
   ```

   The frontend will be accessible at http://localhost:5173 (or the port shown in your terminal).

## Testing
1. API endpints : ( From swagger/postman/front-app)
![image](https://github.com/user-attachments/assets/46fc95b1-b442-4797-ae54-ef5219e734ef)
```
POST /Jobs
Request body:
{
  "name": "string",
  "priority": 0
}

GET /Jobs
No request body.

DELETE /Jobs/{jobID}
No request body.

DELETE /Jobs/status/{status}
No request body.

POST /Jobs/{jobID}/stop
No request body.

POST /Jobs/{jobID}/restart
No request body.
```
3. Job stress-test : The backend includes a folder named 'Queries' which as a query for creating 40 jobs at once.  
   Run the query on the DB and restart the worker-service (or all), what will trigger the backup recovery worflow.
   Another way is to add jobs manually in a fast paced.

## Troubleshooting

- **SignalR connection issues**: Verify that the BaseUrl and HubPath are correctly configured in appsettings.json.
- **API errors**: Check the backend logs for detailed error messages, especially for database or job processing issues.
- **Frontend issues**: Check the browser console for any errors related to the frontend, and ensure that the backend is running.
