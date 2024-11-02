Imports System.Windows.Forms

Public Class CreateSuperuserForm
    Inherits Form

    Private usernameLabel As New Label
    Private usernameTextBox As New TextBox
    Private emailLabel As New Label
    Private emailTextBox As New TextBox
    Private passwordLabel As New Label
    Private passwordTextBox As New TextBox
    Private createButton As New Button
    Private cancelButton As New Button

    Public Property Username As String
    Public Property Email As String
    Public Property Password As String

    Public Sub New()
        Me.Text = "Create Superuser"
        Me.Size = New Size(300, 250)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent

        ' Username
        usernameLabel.Text = "Username:"
        usernameLabel.Location = New Point(20, 20)
        usernameLabel.AutoSize = True
        Me.Controls.Add(usernameLabel)

        usernameTextBox.Location = New Point(100, 20)
        usernameTextBox.Width = 150
        Me.Controls.Add(usernameTextBox)

        ' Email
        emailLabel.Text = "Email:"
        emailLabel.Location = New Point(20, 60)
        emailLabel.AutoSize = True
        Me.Controls.Add(emailLabel)

        emailTextBox.Location = New Point(100, 60)
        emailTextBox.Width = 150
        Me.Controls.Add(emailTextBox)

        ' Password
        passwordLabel.Text = "Password:"
        passwordLabel.Location = New Point(20, 100)
        passwordLabel.AutoSize = True
        Me.Controls.Add(passwordLabel)

        passwordTextBox.Location = New Point(100, 100)
        passwordTextBox.Width = 150
        passwordTextBox.UseSystemPasswordChar = True
        Me.Controls.Add(passwordTextBox)

        ' Create Button
        createButton.Text = "Create"
        createButton.Location = New Point(40, 150)
        AddHandler createButton.Click, AddressOf CreateButton_Click
        Me.Controls.Add(createButton)

        ' Cancel Button
        cancelButton.Text = "Cancel"
        cancelButton.Location = New Point(150, 150)
        AddHandler cancelButton.Click, AddressOf CancelButton_Click
        Me.Controls.Add(cancelButton)
    End Sub

    Private Sub CreateButton_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(usernameTextBox.Text) OrElse
           String.IsNullOrWhiteSpace(emailTextBox.Text) OrElse
           String.IsNullOrWhiteSpace(passwordTextBox.Text) Then
            MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Username = usernameTextBox.Text
        Email = emailTextBox.Text
        Password = passwordTextBox.Text

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub CancelButton_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()
        '
        'CreateSuperuserForm
        '
        Me.ClientSize = New System.Drawing.Size(282, 253)
        Me.Name = "CreateSuperuserForm"
        Me.ResumeLayout(False)

    End Sub

    Private Sub CreateSuperuserForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class
