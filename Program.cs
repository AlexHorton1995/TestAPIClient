using System;
using System.Net;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;


namespace TestAPIClient
{
    internal class Program : IDisposable
    {
        internal static SqlConnection SqlConn { get; set; }

        //I showed you how to get the connection string here :)
        internal const string ConnString = "";

        internal static DataTable DTable { get; set; }

        internal static void Main(string[] args)
        {
            //new step one - create a data table!
            DTable = CreateDataTable();

            //step two - create method to get the data from API
            var data = GotRequest();

            //step three - load API data to temp Data Table;
            PopulateDatatable(DTable, data);

            //step four - load data to live table.
            BulkCopyToTable(DTable);
        }

        /// <summary>
        /// consume the web api here
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<DataModel> GotRequest()
        {
            //create an empty List for the DataModels we'll get back from API
            List<DataModel> DataList = new List<DataModel>();

            //INSTANTIATE to null here so that we can always pass back something to 
            //check for!
            DataList = null;

            //next - give us a container to hold our JSON response in.
            string response = string.Empty;

            //This is the URL of the soap API we are hitting.
            //keep in mind - this is a public API, which is not safe 
            //usually (ask Parler, lol)
            string soapUrl = "https://jsonplaceholder.typicode.com/comments";

            //use the Using keyword here to properly dispose of objects.
            using (WebClient client = new WebClient())
            {
                try
                {
                    //this is us hitting the actual REST API.
                    response = client.DownloadString(soapUrl);

                    //and here, we are parsing out our response into 
                    //our data models!
                    DataList = JsonConvert.
                        DeserializeObject<List<DataModel>>(response);

                }
                catch (Exception ex)
                {
                    //if we have an issue, log out the issue to the 
                    //screen!
                    Console.WriteLine($"There was an exception: {ex.Message}");
                }
            }

            return DataList;
        }

        /// <summary>
        /// CreateDataTable method creates a data table for bulk load
        /// </summary>
        /// <returns></returns>
        internal static DataTable CreateDataTable()
        {
            /*
             okie dokie, I'm back!
             */
            DataTable dataTable = new DataTable("TestAPIClient");
            dataTable.Columns.AddRange
            (
                new DataColumn[]
                {
                    new DataColumn("id", typeof(int)) { AllowDBNull = false },
                    new DataColumn("postId", typeof(short)) { AllowDBNull = false },
                    new DataColumn("name", typeof(string)) { MaxLength = 60 },
                    new DataColumn("email", typeof(string)) { MaxLength = 60 },
                    new DataColumn("body", typeof(string)) { MaxLength = 256 }
                }
            );

            return dataTable;
        }



        internal static void PopulateDatatable(DataTable dt, IEnumerable<DataModel> models)
        {
            int dataCount = 0;
            try
            {

                //something is going on here - I should have 500 rows in my table...
                foreach (var model in models)
                {
                    DataRow row = dt.NewRow();
                    row["id"] = model.ID;
                    row["postId"] = model.PostID;
                    row["name"] = model.Name.Length > 60 ? model.Name.Substring(0, 60) : model.Name;
                    row["email"] = model.EmailAddress;
                    row["body"] = model.Body.Length > 255 ? model.Body.Substring(0, 255) : model.Body;
                    dt.Rows.Add(row);
                    dataCount++;
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Exception while adding row {dataCount}: {ex.Message}");
            }
        }

        /// <summary>
        /// Bulk copies data to table.  Got to create the table first!
        /// </summary>
        /// <returns></returns>
        internal static bool BulkCopyToTable(DataTable dt)
        {
            bool success = false;

            using (SqlConn = new SqlConnection(ConnString))
            {
                SqlConn.Open();  //this opens the connection to the DB.

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(SqlConn))
                {
                    bulkCopy.DestinationTableName = "dbo.TestAPIClient";
                    try
                    {
                        bulkCopy.WriteToServer(dt);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception during copy: {ex.Message}");
                    }
                }
            }
            return success;
        }


        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (DTable != null)
            {
                if (disposing)
                {
                    if (DTable.Rows.Count > 0)
                    {
                        DTable.Dispose();
                    }
                }
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Program()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    #region DataModel

    /// <summary>
    /// This Datamodel is Used to populate our data from service
    /// </summary>
    [DataContract]
    class DataModel
    {

        [DataMember(Name = "id")]
        public int ID { get; set; }

        [DataMember(Name = "postId")]
        public short PostID { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "email")]
        public string EmailAddress { get; set; }

        [DataMember(Name = "body")]
        public string Body { get; set; }

    }
    #endregion

}
