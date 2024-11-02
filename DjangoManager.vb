Imports System.Diagnostics
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading

' Define Enum outside the class
Public Enum DjangoMessageType
    Normal = 0
    [Error] = 1
    Warning = 2
    Success = 3
End Enum

' Define delegate for the event
Public Delegate Sub OutputReceivedEventHandler(message As String, messageType As DjangoMessageType)

Public Class DjangoManager
    ' Events with proper delegates
    Public Event OutputReceived As OutputReceivedEventHandler
    Public Event ServerStatusChanged(isRunning As Boolean)

    Private djangoProcess As Process
    Private pythonPath As String = "python"
    Private _projectPath As String
    Private _isRunning As Boolean = False
    Private _isShuttingDown As Boolean = False

    Public Property ProjectPath As String
        Get
            Return _projectPath
        End Get
        Set(value As String)
            _projectPath = value
        End Set
    End Property

    Public ReadOnly Property IsRunning As Boolean
        Get
            Return _isRunning
        End Get
    End Property

    Public Sub New(projectPath As String)
        Me.ProjectPath = projectPath
    End Sub

    Public Sub StartServer(host As String, port As String)
        If _isRunning Then
            Return
        End If

        Try
            djangoProcess = New Process()
            With djangoProcess.StartInfo
                .FileName = pythonPath
                .Arguments = $"manage.py runserver {host}:{port}"
                .UseShellExecute = False
                .RedirectStandardOutput = True
                .RedirectStandardError = True
                .CreateNoWindow = True
                .WorkingDirectory = ProjectPath
                .StandardOutputEncoding = System.Text.Encoding.UTF8
                .StandardErrorEncoding = System.Text.Encoding.UTF8
            End With

            AddHandler djangoProcess.OutputDataReceived, AddressOf ProcessOutputHandler
            AddHandler djangoProcess.ErrorDataReceived, AddressOf ProcessErrorHandler
            AddHandler djangoProcess.Exited, AddressOf ProcessExitedHandler

            djangoProcess.EnableRaisingEvents = True
            djangoProcess.Start()
            djangoProcess.BeginOutputReadLine()
            djangoProcess.BeginErrorReadLine()

            _isRunning = True
            RaiseEvent ServerStatusChanged(True)
            RaiseEvent OutputReceived($"Starting Django server at {host}:{port}...", DjangoMessageType.Success)
        Catch ex As Exception
            _isRunning = False
            RaiseEvent ServerStatusChanged(False)
            RaiseEvent OutputReceived($"Failed to start server: {ex.Message}", DjangoMessageType.Error)
        End Try
    End Sub

    Public Sub StopServer()
        Try
            If djangoProcess IsNot Nothing AndAlso Not djangoProcess.HasExited Then
                _isShuttingDown = True

                Try
                    ' Kill the entire process tree to ensure all child processes are terminated
                    KillProcessTree(djangoProcess.Id)
                    ' Wait briefly to allow processes to terminate
                    Thread.Sleep(1000)
                    RaiseEvent OutputReceived("Server stopped.", DjangoMessageType.Success)
                Catch ex As Exception
                    RaiseEvent OutputReceived($"Error during shutdown: {ex.Message}", DjangoMessageType.Warning)
                Finally
                    Try
                        If Not djangoProcess.HasExited Then
                            djangoProcess.Close()
                        End If
                    Catch ex As Exception
                        ' Ignore errors during process closing
                    End Try
                End Try
            End If

        Catch ex As Exception
            RaiseEvent OutputReceived($"Failed to stop server: {ex.Message}", DjangoMessageType.Error)
        Finally
            _isRunning = False
            _isShuttingDown = False
            RaiseEvent ServerStatusChanged(False)
            djangoProcess = Nothing
        End Try
    End Sub

    Private Sub KillProcessTree(processId As Integer)
        Try
            ' Use taskkill to kill the process tree including all child processes
            Using killProcess As New Process()
                With killProcess.StartInfo
                    .FileName = "taskkill"
                    .Arguments = $"/PID {processId} /T /F"
                    .UseShellExecute = False
                    .CreateNoWindow = True
                    .RedirectStandardOutput = True
                    .RedirectStandardError = True
                End With

                killProcess.Start()
                killProcess.WaitForExit(5000)
            End Using
        Catch ex As Exception
            RaiseEvent OutputReceived($"Error killing process tree: {ex.Message}", DjangoMessageType.Error)
        End Try
    End Sub

    Private Sub ProcessDataReceived(data As String)
        If Not String.IsNullOrWhiteSpace(data) Then
            Dim msgType As DjangoMessageType = DjangoMessageType.Normal

            If data.IndexOf("[ERROR]", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
           data.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
           data.IndexOf("Not Found:", StringComparison.OrdinalIgnoreCase) >= 0 Then
                msgType = DjangoMessageType.Error
            ElseIf data.IndexOf("[WARNING]", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
               data.IndexOf("warning", StringComparison.OrdinalIgnoreCase) >= 0 Then
                msgType = DjangoMessageType.Warning
            ElseIf data.IndexOf("[INFO]", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
               data.IndexOf("GET ", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
               data.IndexOf("POST ", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
               data.IndexOf("Watching for file changes", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
               data.IndexOf("Starting development server", StringComparison.OrdinalIgnoreCase) >= 0 Then
                msgType = DjangoMessageType.Success
            Else
                msgType = DjangoMessageType.Normal
            End If

            RaiseEvent OutputReceived(data, msgType)
        End If
    End Sub
    Private Sub ProcessOutputHandler(sender As Object, e As DataReceivedEventArgs)
        If e.Data IsNot Nothing Then
            ProcessDataReceived(e.Data)
        End If
    End Sub


    Private Sub ProcessErrorHandler(sender As Object, e As DataReceivedEventArgs)
        If e.Data IsNot Nothing Then
            ProcessDataReceived(e.Data)
        End If
    End Sub

    Private Sub ProcessExitedHandler(sender As Object, e As EventArgs)
        If Not _isShuttingDown Then
            _isRunning = False
            RaiseEvent ServerStatusChanged(False)
            RaiseEvent OutputReceived("Django server process exited.", DjangoMessageType.Warning)
            djangoProcess = Nothing
        End If
    End Sub

    Public Sub RunMakemigrations()
        RunManagementCommand("makemigrations")
    End Sub

    Public Sub RunMigrate()
        RunManagementCommand("migrate")
    End Sub

    Public Sub RunCreateSuperuser(username As String, email As String, password As String)
        ' Run the createsuperuser command with the provided details
        Dim arguments = $"createsuperuser --username {username} --email {email} --no-input"
        Dim env = New Dictionary(Of String, String) From {
        {"DJANGO_SUPERUSER_PASSWORD", password}
    }
        RunManagementCommand(arguments, env)
    End Sub
    Public Sub RunStartApp(appName As String)
        RunManagementCommand($"startapp {appName}")
        AddAppToSettings(appName)
    End Sub

    Public Sub AddAppToSettings(appName As String)
        Try
            ' Путь к manage.py
            Dim managePyPath As String = Path.Combine(ProjectPath, "manage.py")

            If Not File.Exists(managePyPath) Then
                RaiseEvent OutputReceived("File manage.py not found.", DjangoMessageType.Error)
                Return
            End If

            ' Читаем manage.py и ищем строку с DJANGO_SETTINGS_MODULE
            Dim managePyLines As String() = File.ReadAllLines(managePyPath)
            Dim settingsModuleLine As String = managePyLines.FirstOrDefault(Function(line) line.Contains("DJANGO_SETTINGS_MODULE"))

            If String.IsNullOrEmpty(settingsModuleLine) Then
                RaiseEvent OutputReceived("DJANGO_SETTINGS_MODULE variable not found in manage.py.", DjangoMessageType.Error)
                Return
            End If

            ' Извлекаем значение DJANGO_SETTINGS_MODULE
            Dim settingsModule As String = ""
            Dim pattern As String = "os\.environ\.setdefault\('DJANGO_SETTINGS_MODULE',\s*'([^']+)'\)"
            Dim match As Match = Regex.Match(settingsModuleLine, pattern)

            If match.Success Then
                settingsModule = match.Groups(1).Value
            Else
                RaiseEvent OutputReceived("Не удалось извлечь DJANGO_SETTINGS_MODULE из manage.py.", DjangoMessageType.Error)
                Return
            End If

            ' Преобразуем модуль в путь к файлу settings.py
            Dim settingsPathParts As String() = settingsModule.Split("."c)
            Dim settingsDirectory As String = Path.Combine(ProjectPath, Path.Combine(settingsPathParts.Take(settingsPathParts.Length - 1).ToArray()))
            Dim settingsFileName As String = settingsPathParts.Last() & ".py"
            Dim settingsPath As String = Path.Combine(settingsDirectory, settingsFileName)

            If Not File.Exists(settingsPath) Then
                RaiseEvent OutputReceived($"File settings.py not found at path {settingsPath}.", DjangoMessageType.Error)
                Return
            End If

            ' Создаем резервную копию файла settings.py
            Dim backupSettingsPath As String = settingsPath & ".bak"
            File.Copy(settingsPath, backupSettingsPath, True)
            RaiseEvent OutputReceived($"Backup of settings.py created: {backupSettingsPath}", DjangoMessageType.Success)

            ' Читаем содержимое settings.py
            Dim settingsContent As String = File.ReadAllText(settingsPath)
            Dim installedAppsPattern As String = "(?s)(INSTALLED_APPS\s*=\s*\[\s*)(.*?)(\s*\])"

            ' Ищем секцию INSTALLED_APPS
            Dim installedAppsMatch As Match = Regex.Match(settingsContent, installedAppsPattern)

            If installedAppsMatch.Success Then
                Dim beforeApps As String = installedAppsMatch.Groups(1).Value
                Dim appsList As String = installedAppsMatch.Groups(2).Value
                Dim afterApps As String = installedAppsMatch.Groups(3).Value

                ' Формируем новую строку с приложением
                Dim newAppEntry As String = $"    #'{appName}'," & vbCrLf

                ' Добавляем новую строку перед закрывающей скобкой
                appsList = appsList.TrimEnd() & vbCrLf & newAppEntry

                ' Собираем новое содержимое файла
                Dim newSettingsContent As String = settingsContent.Substring(0, installedAppsMatch.Index) &
                                               beforeApps & appsList & afterApps &
                                               settingsContent.Substring(installedAppsMatch.Index + installedAppsMatch.Length)

                ' Записываем изменения обратно в settings.py
                File.WriteAllText(settingsPath, newSettingsContent)
                RaiseEvent OutputReceived($"Application '{appName}' added to settings.py as commented.", DjangoMessageType.Success)
            Else
                RaiseEvent OutputReceived("Section INSTALLED_APPS not found or has non-standard format in settings.py.", DjangoMessageType.Error)
            End If
        Catch ex As Exception
            RaiseEvent OutputReceived($"Error updating settings.py: {ex.Message}", DjangoMessageType.Error)
        End Try
    End Sub



    Private Sub RunManagementCommand(command As String, Optional envVariables As Dictionary(Of String, String) = Nothing)
        Try
            Dim process As New Process()
            With process.StartInfo
                .FileName = pythonPath
                .Arguments = $"manage.py {command}"  ' Corrected line
                .UseShellExecute = False
                .RedirectStandardOutput = True
                .RedirectStandardError = True
                .CreateNoWindow = True
                .WorkingDirectory = ProjectPath
                .StandardOutputEncoding = System.Text.Encoding.UTF8
                .StandardErrorEncoding = System.Text.Encoding.UTF8
            End With

            If envVariables IsNot Nothing Then
                For Each kvp In envVariables
                    process.StartInfo.EnvironmentVariables(kvp.Key) = kvp.Value
                Next
            End If

            AddHandler process.OutputDataReceived, AddressOf ProcessOutputHandler
            AddHandler process.ErrorDataReceived, AddressOf ProcessErrorHandler

            process.Start()
            process.BeginOutputReadLine()
            process.BeginErrorReadLine()
            process.WaitForExit()

            RaiseEvent OutputReceived($"{command} completed.", DjangoMessageType.Success)
        Catch ex As Exception
            RaiseEvent OutputReceived($"Failed to run {command}: {ex.Message}", DjangoMessageType.Error)
        End Try
    End Sub
End Class
