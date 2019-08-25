Imports System
Imports Newtonsoft.Json
Imports System.Data
Imports System.Net.Http
Imports System.Text
Imports System.Linq
Imports MySql.Data.MySqlClient

' Install System.Data.DataSetExtensions to Access AsEnumerable()

Namespace integrationExampleUsingCsharp
    Class Program
        Private Const RequestUri As String = "" ' Endpoint URL (e.g: https://nana.sa/api/sync_store_products_by_key)
        Private Const Token As String = "" ' JWT Token (Token will be provided for each branch individually)
        Private Const query As String = "" ' Query String (e.g select unit_price as price, item_number as sku, barcode as barcode from products)

        Public Shared Sub Main(ByVal args As String())
            Dim connStr As String = "server=localhost;database=database;uid=root;pwd=root" ' Connection String
            Using conn As MySqlConnection = New MySqlConnection(connStr)
                Try
                    Console.WriteLine("Connecting To MySql ...")
                    Using da As MySqlDataAdapter = New MySqlDataAdapter() ' Generate The MySql Adapter
                        Using dt As DataTable = New DataTable()
                            Using sqlCommand As MySqlCommand = conn.CreateCommand() ' Create MySql Command
                                sqlCommand.CommandType = CommandType.Text
                                sqlCommand.CommandText = query
                                da.SelectCommand = sqlCommand ' Execute MySql Command
                                da.Fill(dt) ' Dump Result into DataTable
                                sqlCommand.Dispose()
                                da.Dispose()
                                Console.WriteLine("Preparing Batches")
                                Dim tr = dt.Rows.Count ' Get Total Rows Count
                                tr = CInt(Math.Ceiling(CDbl(tr) / 1000)) ' Divide total by the batches
                                Dim skipCount = 0

                                For i As Integer = 0 To tr - 1 ' Loop through the total table rows
                                    Console.WriteLine("Sending " & (i + 1) & " Batch")
                                    Dim dataBatch = dt.AsEnumerable().Skip(skipCount).Take(1000) ' Pick 1000 from the total results
                                    Dim copyTableData = dataBatch.CopyToDataTable() ' Copy batched to new datatable
                                    Dim jsonResult = JsonConvert.SerializeObject(New With {Key.product_arrays = copyTableData}) ' Serialize Copied Data To JSON Object (product_arrays key for product_sync and barcode_arrays for daily sales)
                                    sendRequest(jsonResult)
                                    skipCount += 1000 ' Increase skipCount by 1000 to pick the next batch
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
                    client.DefaultRequestHeaders.Add("Authorization", Token) ' Add Authorization Header defined on line 12

                    Using content As HttpContent = New StringContent(data, Encoding.UTF8, "application/json") ' Prepare JSON Payload To Send

                        Using response As HttpResponseMessage = client.PostAsync(RequestUri, content).Result ' Send HTTP request and store result in (response)
                            Console.WriteLine(response.Content.ReadAsStringAsync().Result) ' Print Out Response
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
