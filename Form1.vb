Imports System.Threading.Tasks
Imports System.Drawing
Imports System.Windows.Forms
Imports System.IO
Imports System.Linq
Imports System.Threading

Public Class Form1
    Inherits Form

    Private WithEvents ButtonStart As New Button
    Private WithEvents ButtonStop As New Button
    Private WithEvents TextBoxOutput As New RichTextBox
    Private WithEvents StatusLabel As New Label
    Private WithEvents HostInput As New TextBox
    Private WithEvents PortInput As New TextBox
    Private WithEvents HostLabel As New Label
    Private WithEvents PortLabel As New Label
    Private resetButton As New Button
    Private sidebarPanel As New Panel
    Private WithEvents trayIcon As New NotifyIcon
    Private djangoManager As DjangoManager
    Private projectResetter As ProjectResetter

    Private Sub InitializeComponents()
        ' Set the form to fixed size and disable resizing
        Me.Size = New Size(1000, 600)
        Me.Text = "Django Server Controller"
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        ' NotifyIcon
        trayIcon.Icon = Me.Icon ' Используем иконку приложения или укажите другую
        trayIcon.Visible = False ' Изначально скрыт
        trayIcon.Text = "Django Server Controller"

        ' Создаем контекстное меню для иконки в трее
        Dim trayMenu As New ContextMenu()
        trayMenu.MenuItems.Add("Open", AddressOf TrayIcon_Open)
        trayMenu.MenuItems.Add("Close", AddressOf TrayIcon_Exit)
        trayIcon.ContextMenu = trayMenu


        ' Host Label and Input
        HostLabel.Text = "Host:"
        HostLabel.AutoSize = True
        HostLabel.Location = New Point(360, 25)
        Me.Controls.Add(HostLabel)

        HostInput.Text = "0.0.0.0"
        HostInput.Size = New Size(100, 20)
        HostInput.Location = New Point(400, 25)
        Me.Controls.Add(HostInput)

        ' Port Label and Input
        PortLabel.Text = "Port:"
        PortLabel.AutoSize = True
        PortLabel.Location = New Point(510, 25)
        Me.Controls.Add(PortLabel)

        PortInput.Text = "8000"
        PortInput.Size = New Size(60, 20)
        PortInput.Location = New Point(550, 25)
        Me.Controls.Add(PortInput)

        ' Status Label
        StatusLabel.Text = "Server Status: Stopped"
        StatusLabel.AutoSize = True
        StatusLabel.Location = New Point(620, 25)
        Me.Controls.Add(StatusLabel)

        ' RichTextBox Output
        TextBoxOutput.Multiline = True
        TextBoxOutput.ScrollBars = RichTextBoxScrollBars.Vertical
        TextBoxOutput.Size = New Size(760, 480)
        TextBoxOutput.Location = New Point(20, 60)
        TextBoxOutput.ReadOnly = True
        TextBoxOutput.Font = New Font("Consolas", 10)
        TextBoxOutput.BackColor = Color.Black
        TextBoxOutput.ForeColor = Color.LightGray
        Me.Controls.Add(TextBoxOutput)

        ' Start Button
        ButtonStart.Text = "Start Django Server"
        ButtonStart.Size = New Size(150, 30)
        ButtonStart.Location = New Point(20, 20)
        Me.Controls.Add(ButtonStart)

        ' Stop Button
        ButtonStop.Text = "Stop Django Server"
        ButtonStop.Size = New Size(150, 30)
        ButtonStop.Location = New Point(190, 20)
        ButtonStop.Enabled = False
        Me.Controls.Add(ButtonStop)

        ' Sidebar panel
        sidebarPanel.Size = New Size(180, 480)
        sidebarPanel.Location = New Point(800, 60)
        Me.Controls.Add(sidebarPanel)

        ' Reset Button
        resetButton.Text = "Reset Project"
        resetButton.Size = New Size(150, 30)
        resetButton.Location = New Point(15, 20)
        AddHandler resetButton.Click, AddressOf ResetButton_Click
        sidebarPanel.Controls.Add(resetButton)
        ' Startapp Button
        Dim startappButton As New Button
        startappButton.Text = "Start App"
        startappButton.Size = New Size(150, 30)
        startappButton.Location = New Point(15, 180)
        AddHandler startappButton.Click, AddressOf StartappButton_Click
        sidebarPanel.Controls.Add(startappButton)

        ' Makemigrations Button
        Dim makemigrationsButton As New Button
        makemigrationsButton.Text = "Makemigrations"
        makemigrationsButton.Size = New Size(150, 30)
        makemigrationsButton.Location = New Point(15, 60)
        AddHandler makemigrationsButton.Click, AddressOf MakemigrationsButton_Click
        sidebarPanel.Controls.Add(makemigrationsButton)

        ' Migrate Button
        Dim migrateButton As New Button
        migrateButton.Text = "Migrate"
        migrateButton.Size = New Size(150, 30)
        migrateButton.Location = New Point(15, 100)
        AddHandler migrateButton.Click, AddressOf MigrateButton_Click
        sidebarPanel.Controls.Add(migrateButton)

        ' Create Superuser Button
        Dim createSuperuserButton As New Button
        createSuperuserButton.Text = "Create Superuser"
        createSuperuserButton.Size = New Size(150, 30)
        createSuperuserButton.Location = New Point(15, 140)
        AddHandler createSuperuserButton.Click, AddressOf CreateSuperuserButton_Click
        sidebarPanel.Controls.Add(createSuperuserButton)
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim projectPath As String = Application.StartupPath
        Dim managePyPath As String = Path.Combine(projectPath, "manage.py")
        If Not File.Exists(managePyPath) Then
            MessageBox.Show("The 'manage.py' file could not be found in the application's directory. Please ensure it is in the correct location.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Me.Close()
            Application.Exit()
            Return
        End If
        InitializeComponents()
        ' Replace with your actual Django project path
        djangoManager = New DjangoManager(projectPath)
        projectResetter = New ProjectResetter(projectPath, AddressOf AppendOutput)

        AddHandler djangoManager.OutputReceived, AddressOf HandleDjangoOutput
        AddHandler djangoManager.ServerStatusChanged, AddressOf HandleServerStatusChanged
    End Sub

    Private Sub ButtonStart_Click(sender As Object, e As EventArgs) Handles ButtonStart.Click
        djangoManager.StartServer(HostInput.Text, PortInput.Text)
    End Sub
    Private Sub StartappButton_Click(sender As Object, e As EventArgs)
        ' Открываем диалоговое окно для ввода названия приложения
        Dim appName As String = InputBox("Enter application name:", "Create New App")

        ' Проверяем, что название не пустое
        If String.IsNullOrWhiteSpace(appName) Then
            MessageBox.Show("Application name cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        ' Отключаем кнопки во время выполнения команды
        ButtonStart.Enabled = False
        ButtonStop.Enabled = False
        resetButton.Enabled = False

        Task.Run(Sub()
                     djangoManager.RunStartApp(appName)
                     Me.Invoke(Sub()
                                   ' Включаем кнопки после выполнения команды
                                   ButtonStart.Enabled = Not djangoManager.IsRunning
                                   ButtonStop.Enabled = djangoManager.IsRunning
                                   resetButton.Enabled = True
                               End Sub)
                 End Sub)
    End Sub

    Private Sub MakemigrationsButton_Click(sender As Object, e As EventArgs)
        ButtonStart.Enabled = False
        ButtonStop.Enabled = False
        resetButton.Enabled = False

        Task.Run(Sub()
                     djangoManager.RunMakemigrations()
                     Me.Invoke(Sub()
                                   ButtonStart.Enabled = Not djangoManager.IsRunning
                                   ButtonStop.Enabled = djangoManager.IsRunning
                                   resetButton.Enabled = True
                               End Sub)
                 End Sub)
    End Sub

    Private Sub MigrateButton_Click(sender As Object, e As EventArgs)
        ButtonStart.Enabled = False
        ButtonStop.Enabled = False
        resetButton.Enabled = False

        Task.Run(Sub()
                     djangoManager.RunMigrate()
                     Me.Invoke(Sub()
                                   ButtonStart.Enabled = Not djangoManager.IsRunning
                                   ButtonStop.Enabled = djangoManager.IsRunning
                                   resetButton.Enabled = True
                               End Sub)
                 End Sub)
    End Sub

    Private Sub CreateSuperuserButton_Click(sender As Object, e As EventArgs)
        Using createForm As New CreateSuperuserForm()
            If createForm.ShowDialog(Me) = DialogResult.OK Then
                Dim username = createForm.Username
                Dim email = createForm.Email
                Dim password = createForm.Password

                ButtonStart.Enabled = False
                ButtonStop.Enabled = False
                resetButton.Enabled = False

                Task.Run(Sub()
                             djangoManager.RunCreateSuperuser(username, email, password)
                             Me.Invoke(Sub()
                                           ButtonStart.Enabled = Not djangoManager.IsRunning
                                           ButtonStop.Enabled = djangoManager.IsRunning
                                           resetButton.Enabled = True
                                       End Sub)
                         End Sub)
            End If
        End Using
    End Sub

    Private Sub ButtonStop_Click(sender As Object, e As EventArgs) Handles ButtonStop.Click
        ' Disable controls while stopping
        ButtonStop.Enabled = False
        ButtonStart.Enabled = False
        Task.Run(Sub()
                     djangoManager.StopServer()
                     Me.Invoke(Sub()
                                   ButtonStart.Enabled = True
                                   ButtonStop.Enabled = False
                                   HostInput.Enabled = True
                                   PortInput.Enabled = True
                                   StatusLabel.Text = "Server Status: Stopped"
                               End Sub)
                 End Sub)
    End Sub

    Private Sub HandleDjangoOutput(message As String, messageType As DjangoMessageType)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() HandleDjangoOutput(message, messageType))
            Return
        End If

        TextBoxOutput.SelectionStart = TextBoxOutput.TextLength
        Select Case messageType
            Case DjangoMessageType.Error
                TextBoxOutput.SelectionColor = Color.Red
            Case DjangoMessageType.Warning
                TextBoxOutput.SelectionColor = Color.Yellow
            Case DjangoMessageType.Success
                TextBoxOutput.SelectionColor = Color.LightGreen
            Case Else
                TextBoxOutput.SelectionColor = Color.LightGray
        End Select

        TextBoxOutput.AppendText(message & vbCrLf)
        TextBoxOutput.ScrollToCaret()
    End Sub

    Private Sub HandleServerStatusChanged(isRunning As Boolean)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() HandleServerStatusChanged(isRunning))
            Return
        End If

        ButtonStart.Enabled = Not isRunning
        ButtonStop.Enabled = isRunning
        HostInput.Enabled = Not isRunning
        PortInput.Enabled = Not isRunning
        StatusLabel.Text = If(isRunning, "Server Status: Running", "Server Status: Stopped")
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Проверяем, что форма закрывается пользователем (а не программно)
        If e.CloseReason = CloseReason.UserClosing Then
            ' Отменяем закрытие формы
            e.Cancel = True
            ' Сворачиваем форму
            Me.Hide()
            ' Показываем иконку в трее
            trayIcon.Visible = True
            ' Показываем уведомление из трея
            trayIcon.ShowBalloonTip(1000, "Django Server Controller", "Program minimized to tray. Double-click the icon to restore.", ToolTipIcon.Info)
        Else
            ' Если форма закрывается программно (например, из меню трея), разрешаем закрытие
            If djangoManager IsNot Nothing AndAlso djangoManager.IsRunning Then
                djangoManager.StopServer()
                Thread.Sleep(1000)
            End If
        End If
    End Sub

    Private Sub TrayIcon_Open(sender As Object, e As EventArgs)
        ' Восстанавливаем форму
        Me.Show()
        Me.WindowState = FormWindowState.Normal
        trayIcon.Visible = False
    End Sub

    Private Sub TrayIcon_Exit(sender As Object, e As EventArgs)
        ' Закрываем приложение
        trayIcon.Visible = False
        If djangoManager IsNot Nothing AndAlso djangoManager.IsRunning Then
            djangoManager.StopServer()
            Thread.Sleep(500)
        End If
        Application.Exit()
    End Sub

    Private Sub trayIcon_DoubleClick(sender As Object, e As EventArgs) Handles trayIcon.DoubleClick
        ' Восстанавливаем форму при двойном щелчке по иконке
        TrayIcon_Open(sender, e)
    End Sub
    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        If Me.WindowState = FormWindowState.Minimized Then
            Me.Hide()
            trayIcon.Visible = True
            trayIcon.ShowBalloonTip(300, "Django Server Controller", "Program minimized to tray. Double-click the icon to restore.", ToolTipIcon.Info)
        End If
    End Sub


    Private Sub ResetButton_Click(sender As Object, e As EventArgs)
        ' Disable the reset button to prevent multiple clicks
        resetButton.Enabled = False
        Task.Run(Sub()
                     projectResetter.ResetProject()
                     Me.Invoke(Sub() resetButton.Enabled = True)
                 End Sub)
    End Sub

    Private Sub AppendOutput(message As String)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() AppendOutput(message))
            Return
        End If

        TextBoxOutput.SelectionStart = TextBoxOutput.TextLength
        TextBoxOutput.SelectionColor = Color.LightGray
        TextBoxOutput.AppendText(message & vbCrLf)
        TextBoxOutput.ScrollToCaret()
    End Sub
End Class
