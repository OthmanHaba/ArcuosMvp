# .NET Core Arcuos Test Project Setup

## Setup Project

1.  **Install .NET Core v9.0**

2.  **Edit the `appsettings.json` file**\
    Add your database connection string.

3.  **Run the following commands:**

    ``` bash
    dotnet restore
    dotnet ef database update
    dotnet build
    dotnet run
    ```

4.  **Visit the Application**

    -   Application runs on port **5013**
    -   Visit `/swagger` for API documentation

	
