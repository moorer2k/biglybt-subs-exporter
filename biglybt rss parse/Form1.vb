Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions

Public Class Form1

    Private ReadOnly rssLinks As New Hashtable
    Private ReadOnly magnetLinks As New Hashtable

    Sub parseMagnet(data As String)
        data = data.Replace("\/", "/")
        Dim regMagnet As Regex = New Regex("(magnet:\?xt=urn:btih:.*?)\""")
        Dim intC As Integer = 0
        If regMagnet.IsMatch(data) Then
            For Each match As Match In regMagnet.Matches(data)
                intC += 1
                Dim magLink = WebUtility.UrlDecode(match.Groups(1).Value)
                If Not magnetLinks.ContainsKey(magLink) Then
                    magnetLinks.Add(magLink, intC)
                End If
            Next
        End If
    End Sub

    Sub parseDecodeRSS(data As String)
        data = data.Replace("\/", "/")
        Dim intC As Integer = 0
        Dim regURL As Regex = New Regex("\w+:\/\/[\w@][\w.:@]+\/?[\w\.?=%&=\-@/$,]*")
        Dim regContent As Regex = New Regex("content"":""(.*)""}},")
        If regContent.IsMatch(data) Then
            Dim link = Encoding.UTF8.GetString(Convert.FromBase64String(regContent.Match(data).Groups(1).Value))
            If regURL.IsMatch(link) Then
                Dim rssLink = WebUtility.UrlDecode(regURL.Match(link).Value)
                If Not rssLinks.ContainsKey(rssLink) Then
                    intC += 1
                    rssLinks.Add(rssLink, intC)
                End If
            End If
        End If
    End Sub

    Private Sub ButtonExport_Click(sender As Object, e As EventArgs) Handles ButtonExport.Click

        Dim subsDir As String = TextSubsDir.Text
        ButtonExport.Enabled = False

        Dim tThread As New Threading.Thread(Sub()

                                                Dim files = Directory.GetFiles(subsDir)
                                                Dim intCurrent As Integer = 0
                                                Dim intTotal As Integer = files.Count

                                                For Each fi In files
                                                    Using sr As StreamReader = File.OpenText(fi)
                                                        Dim sb As New StringBuilder()
                                                        sb.Append(sr.ReadToEnd())
                                                        If fi.Contains(".results") Then
                                                            parseMagnet(sb.ToString)
                                                        End If
                                                        If fi.Contains(".biglybt") Then
                                                            parseDecodeRSS(sb.ToString)
                                                        End If
                                                    End Using
                                                    intCurrent += 1
                                                    SetText(Me, Label1, $"Status: Parsing {intCurrent} of {intTotal}.")
                                                Next

                                                Using writer = File.AppendText(Application.StartupPath & "\magnetLinks.txt")
                                                    For Each link In magnetLinks.Keys
                                                        writer.WriteLine(link)
                                                    Next link
                                                End Using

                                                Using writer = File.AppendText(Application.StartupPath & "\rssLinks.txt")
                                                    For Each link In rssLinks.Keys
                                                        writer.WriteLine(link)
                                                    Next link
                                                End Using

                                                SetText(Me, Label1, $"Status: Exported {rssLinks.Count + magnetLinks.Count} links.")

                                                rssLinks.Clear()
                                                magnetLinks.Clear()

                                            End Sub)

        tThread.Start()

    End Sub

    Private Sub ButtonBrowse_Click(sender As Object, e As EventArgs) Handles ButtonBrowse.Click
        FolderBrowserDialog1.Description = "Select the subs folder from exported backup directory."
        FolderBrowserDialog1.ShowDialog()
        If FolderBrowserDialog1.SelectedPath <> "" Then
            TextSubsDir.Text = FolderBrowserDialog1.SelectedPath
            ButtonExport.Enabled = True
        End If
    End Sub

    Delegate Sub SetText_Delegate(Of T As {New, Control})([Form] As Form, [Control] As T, [Text] As String)
    Public Sub SetText(Of T As {New, Control})([Form] As Form, [Control] As T, [Text] As String)
        If [Control].InvokeRequired Then
            Dim MyDelegate As New SetText_Delegate(Of T)(AddressOf SetText)
            [Form].Invoke(MyDelegate, New Object() {[Form], [Control], [Text]})
        Else
            [Control].Text = [Text]
        End If
    End Sub

End Class
