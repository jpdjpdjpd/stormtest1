using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CXmlInvoiceGenerator
{
    internal static class CXmlInvoiceGeneratorConfigurationHelper
    {
        public static CXmlInvoiceGeneratorConfiguration? GetCXmlInvoiceGeneratorConfiguration()
        {
            //read the configuration from the config file into a configuration class
            CXmlInvoiceGeneratorConfiguration? config = new();

            try
            {
                Console.WriteLine("Reading configuration");

                string fullFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, "Configuration", "CXmlInvoiceGenerator.config.json");
                if (File.Exists(fullFilePath))
                {
                    string json = File.ReadAllText(fullFilePath);
                    config = JsonConvert.DeserializeObject<CXmlInvoiceGeneratorConfiguration>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return config;
        }
    }

    internal class CXmlInvoiceGeneratorConfiguration
    {
        //configuration class with basic validation function
        public string OutputFolder = string.Empty;
        public string Purpose = string.Empty;
        public string Operation = string.Empty;
        public Credentials From = new();
        public Credentials To = new();
        public Credentials Sender = new();
        public string UserAgent = string.Empty;

        public bool IsValid()
        {
            bool bOK = true;

            try
            {
                //check that all required configuration parameters have been defined
                if (OutputFolder.Trim().Length == 0)
                {
                    bOK = false;
                    Console.WriteLine("Configuration: Invalid OutputFolder");
                }

                if (Purpose.Trim().Length == 0)
                {
                    bOK = false;
                    Console.WriteLine("Configuration: Invalid Purpose");
                }

                if (Operation.Trim().Length == 0)
                {
                    bOK = false;
                    Console.WriteLine("Configuration: Invalid Operation");
                }

                if ((From.Domain.Trim().Length == 0)
                    || (From.Identity.Trim().Length == 0))
                {
                    bOK = false;
                    Console.WriteLine("Configuration: Invalid From");
                }

                if ((To.Domain.Trim().Length == 0)
                    || (To.Identity.Trim().Length == 0))
                {
                    bOK = false;
                    Console.WriteLine("Configuration: Invalid To");
                }

                if ((Sender.Domain.Trim().Length == 0)
                    || (Sender.Identity.Trim().Length == 0)
                    || (Sender.SharedSecret.Trim().Length == 0))
                {
                    bOK = false;
                    Console.WriteLine("Configuration: Invalid Sender");
                }

                if ((UserAgent.Trim().Length == 0)
                    || (UserAgent.Trim().Length == 0))
                {
                    bOK = false;
                    Console.WriteLine("Configuration: Invalid UserAgent");
                }
            }
            catch (Exception ex)
            {
                bOK = false;
                Console.WriteLine(ex.ToString());
            }

            return bOK;
        }
    }
    internal class Credentials
    {
        public string Domain = string.Empty;
        public string Identity = string.Empty;
        public string SharedSecret = string.Empty;
    }
}
