Imports System.IO
Imports System.Linq
Imports System.Windows.Forms

Public Class ProjectResetter
    Private projectPath As String
    Private outputAction As Action(Of String)

    Public Sub New(projectPath As String, outputAction As Action(Of String))
        Me.projectPath = projectPath
        Me.outputAction = outputAction
    End Sub

    Public Sub ResetProject()
        Try
            ' Delete migration files excluding __init__.py
            Dim migrationDirs = Directory.GetDirectories(projectPath, "migrations", SearchOption.AllDirectories)
            Dim migrationFiles = migrationDirs.SelectMany(Function(dir) Directory.GetFiles(dir, "*.py")) _
                .Where(Function(f) Not Path.GetFileName(f).StartsWith("__init__"))

            outputAction("Deleted migration files:")
            For Each migrationFile In migrationFiles
                File.Delete(migrationFile) ' Corrected line
                outputAction(migrationFile)
            Next

            ' Delete db.sqlite3
            Dim dbPath = Path.Combine(projectPath, "db.sqlite3")
            If File.Exists(dbPath) Then
                File.Delete(dbPath)
                outputAction("Database file has been deleted.")
            Else
                outputAction("Database file not found.")
            End If

            outputAction("RESET DONE")
        Catch ex As Exception
            MessageBox.Show($"Error resetting project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
