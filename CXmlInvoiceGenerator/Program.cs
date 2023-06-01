using DatabaseAccess;
using Microsoft.VisualBasic;
using System.Data;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace CXmlInvoiceGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("New invoice generation run starting at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            GenerateCXMLForNewInvoices();
            Console.WriteLine("New invoice generation run finishing at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        private static void GenerateCXMLForNewInvoices()
        {
            bool bOK = true;

            try
            {
                //read the config
                CXmlInvoiceGeneratorConfiguration config =  CXmlInvoiceGeneratorConfigurationHelper.GetCXmlInvoiceGeneratorConfiguration();
                if ((config == null)
                    || (!config.IsValid()))
                {
                    //halt execution if the configuration is invalid
                    return;
                }

                //ensure the ouput folder exists
                Directory.CreateDirectory(config.OutputFolder);

                //get the 'Invoices'
                Invoices invoiceDB = new();
                DataTable newInvoices = invoiceDB.GetNewInvoices();

                //loop through each 'Invoice' row
                foreach (DataRow row in newInvoices.Rows)
                {
                    bOK = true;

                    string? invoiceId = row["Id"].ToString();
                    if ((invoiceId == null)
                        || (invoiceId.Trim().Length == 0))
                    {
                        bOK = false;
                        Console.WriteLine("Invoice missing InvoiceId");
                    }

                    if (bOK)
                    {
                        try
                        {
                            Console.WriteLine("Loading Invoice: " + invoiceId);

                            //create the cXml Invoice for the invoice
                            cXML invoice = new cXML
                            {
                                payloadID = invoiceId,   //just using InvoiceId for this?
                                timestamp = DateTime.Now.ToString() // not sure if the XML format will be correct. Can change format if required.
                            };

                            Header invHeader = InvoiceDetailHelper.GetHeader(config);
                            Request invRequest = InvoiceDetailHelper.GetRequest(config, row);

                            object[] invItems = new object[] { invHeader, invRequest };
                            invoice.Items = invItems;

                            //save the cXml Invoice to the configured output folder
                            string fullFilePath = System.IO.Path.Combine(config.OutputFolder, invoiceId + ".xml");
                            Console.WriteLine("Wrinting Invoice " + invoiceId + " to file " + fullFilePath);

                            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(invoice.GetType());
                            using (StreamWriter streamWriter = new StreamWriter(fullFilePath))
                            {
                                xmlSerializer.Serialize(streamWriter, invoice);
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Invoice Error: " + invoiceId + " - " + ex.ToString());
                        }
                    }
                }
            }
            catch(Exception ex) 
            {
                Console.WriteLine(ex.ToString());
            }




            // Assume the invoice is raised on the same day you find it, so PaymentTerms is from Today

            // VAT mode is header (overall total) only, not at item level

            // 3) Save the created invoices into a specified output file with the .xml file extension
        }



    }
}