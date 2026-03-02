# Create Service
sc.exe create "Elementum-WorkerService" binpath= "C:\Users\Melina\source\repos\Elementum\Elementum-WorkerService\Elementum-WorkerService\bin\Release\net10.0\publish\win-x64\Elementum_WorkerService.exe" start= auto

# Start Service
sc.exe start "Elementum-WorkerService"

# Stop Service
sc.exe stop "Elementum-WorkerService"

# Delete Service
sc.exe delete "Elementum-WorkerService"