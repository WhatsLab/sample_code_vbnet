Imports System
Imports Newtonsoft.Json
Imports System.Data
Imports System.Net.Http
Imports System.Text
Imports System.Linq
Imports MySql.Data.MySqlClient

Namespace integrationExampleUsingCsharp
    Class Program
        Private Const RequestUri As String = "" ' nana enpoint URL e.g (https://nana.sa/api/sync_store_products_by_key)
        Private Shared getBranchQuery As String = "" ' query for branches and corresponding tokens
        Private Shared StoreId As String = "" ' local branch Id
        Private Shared Token As String = "" ' corresponding token
        Private Shared getDataQuery As String = "" ' query records either changed products or daily sales

        Public Shared Sub Main(ByVal args As String())
            Dim connStr As String = "server=localhost;database=dotnet;uid=root;pwd=root" ' connection string to the database
            Using conn As MySqlConnection = New MySqlConnection(connStr) ' initiate connection
                Try
                    Console.WriteLine("Connecting To MySql ...")
                    Using da As MySqlDataAdapter = New MySqlDataAdapter() ' create new instance for the mysql adapter
                        Using dt1 As DataTable = New DataTable() ' initiate new data table
                            Using sqlCommand1 As MySqlCommand = conn.CreateCommand() ' prepare new sql command
                                getBranchQuery = "" ' write query text
                                ' query example (select name, token from stores)
                                sqlCommand1.CommandType = CommandType.Text ' define command type
                                sqlCommand1.CommandText = getBranchQuery ' add command text
                                da.SelectCommand = sqlCommand1 ' execute command
                                da.Fill(dt1) ' fill record to the data table declared above
                                sqlCommand1.Dispose()
                                da.Dispose()

                                For Each row As DataRow In dt1.Rows ' foreach branch get either changed records or daily sales
                                    Token = row("token").ToString() ' assign store token
                                    StoreId = row("name").ToString() ' assign store id
                                    Using dt As DataTable = New DataTable() ' initiate new data table
                                        Using sqlCommand As MySqlCommand = conn.CreateCommand() ' prepare new sql command
                                            getDataQuery = "" ' write query text
                                            ' query example (select name, phone, store_id from users where store_id='" & StoreId & "')
                                            sqlCommand.CommandType = CommandType.Text ' define command type
                                            sqlCommand.CommandText = getDataQuery ' add command text
                                            da.SelectCommand = sqlCommand ' execute command
                                            da.Fill(dt) ' fill record to the data table declared above
                                            sqlCommand.Dispose()
                                            da.Dispose()
                                            Console.WriteLine("Preparing Batches")
                                            Dim tr = dt.Rows.Count ' get total records
                                            tr = CInt(Math.Ceiling(CDbl(tr) / 10)) ' divide batches 10 per each
                                            Dim skipCount = 0 ' prepare skipping records 

                                            For i As Integer = 0 To tr - 1 ' loop through records
                                                Console.WriteLine("Sending " & (i + 1) & " Batch") 
                                                Dim dataBatch = dt.AsEnumerable().Skip(skipCount).Take(10) ' take every tr number and skip
                                                Dim copyTableData = dataBatch.CopyToDataTable() ' copy batched to new datatable
                                                Dim jsonResult = JsonConvert.SerializeObject(New With {Key.product_arrays = copyTableData}) ' serialize copied data to JSON object (product_arrays key for product_sync and barcode_arrays for daily sales)
                                                sendRequest(jsonResult) ' send request
                                                skipCount += 10 ' skip the next batch
                                            Next
                                        End Using
                                    End Using
                                Next
                            End Using
                        End Using
                    End Using

                Catch e As Exception
                    Console.WriteLine(e.Message)
                End Try
            End Using
            Console.WriteLine("")
            Console.WriteLine("Done ...")
        End Sub

        Private Shared Sub sendRequest(ByVal data As String)
            Try
                Using client As HttpClient = New HttpClient()
                    client.DefaultRequestHeaders.Add("Authorization", Token) ' append token got from stores query
                    Using content As HttpContent = New StringContent(data, Encoding.UTF8, "application/json") ' prepare data and headers
                        Using response As HttpResponseMessage = client.PostAsync(RequestUri, content).Result
                            Console.WriteLine(response.Content.ReadAsStringAsync().Result)
                            Console.WriteLine("Batch Sent")
                        End Using
                    End Using
                End Using
            Catch e As Exception
                Console.WriteLine(e.Message)
            End Try
        End Sub
    End Class
End Namespace
