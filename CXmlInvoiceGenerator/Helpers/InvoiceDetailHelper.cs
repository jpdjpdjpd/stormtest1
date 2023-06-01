using DatabaseAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CXmlInvoiceGenerator
{
    internal static class InvoiceDetailHelper
    {
        #region enums

        private enum ContactRole
        {
            cRole_soldTo,
            cRole_billTo
        }

        #endregion enums

        public static Header GetHeader(CXmlInvoiceGeneratorConfiguration config)
        {
            //create and return the CXml Header
            Console.WriteLine("Get the CXml Header");
            Header invHeader = new Header();

            try
            {
                XmlDocument doc = new XmlDocument();

                //create the Header-From
                XmlNode fromIdNode = doc.CreateNode("text", "Identity", "");
                fromIdNode.Value = config.From.Identity;

                XmlNode[] fromIdNodes = new XmlNode[] { fromIdNode };
                Identity fromId = new Identity { Any = fromIdNodes };

                Credential fromCred = new Credential
                {
                    domain = config.From.Domain,
                    Identity = fromId
                };

                Credential[] fromCreds = new Credential[] { fromCred };

                From invFrom = new From
                {
                    Credential = fromCreds
                };

                //create the Header-To
                XmlNode toIdNode = doc.CreateNode("text", "Identity", "");
                toIdNode.Value = config.To.Identity;

                XmlNode[] toIdNodes = new XmlNode[] { toIdNode };
                Identity toId = new Identity { Any = toIdNodes };

                Credential toCred = new Credential
                {
                    domain = config.To.Domain,
                    Identity = toId
                };

                Credential[] toCreds = new Credential[] { toCred };

                To invTo = new To
                {
                    Credential = toCreds
                };

                //create the Header-Sender
                XmlNode senderIdNode = doc.CreateNode("text", "Identity", "");
                senderIdNode.Value = config.Sender.Identity;
                XmlNode[] senderIdNodes = new XmlNode[] { senderIdNode };
                Identity senderId = new Identity { Any = senderIdNodes };

                XmlNode senderSharedSecretNode = doc.CreateNode("text", "Identity", "");
                senderSharedSecretNode.Value = config.Sender.SharedSecret;
                XmlNode[] senderSharedSecretNodes = new XmlNode[] { senderSharedSecretNode };
                SharedSecret invSharedSecret = new SharedSecret { Any = senderSharedSecretNodes };

                Credential senderCred = new Credential
                {
                    domain = config.Sender.Domain,
                    Identity = senderId,
                    Item = invSharedSecret
                };

                Credential[] senderCreds = new Credential[] { senderCred };

                Sender invSender = new Sender
                {
                    Credential = senderCreds,
                    UserAgent = config.UserAgent
                };

                //create the Header
                invHeader.From = invFrom;
                invHeader.To = invTo;
                invHeader.Sender = invSender;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return invHeader;
        }
        public static Request GetRequest(CXmlInvoiceGeneratorConfiguration config, DataRow row)
        {
            //create and return the CXml Request
            Console.WriteLine("Get the CXml Request for Invoice " + row["Id"].ToString());

            Request invRequest = new Request
            {
                deploymentMode = RequestDeploymentMode.production   //does this need to be configurable?
            };

            try
            {
                InvoiceDetailRequestHeader invDetReqHdr = GetInvoiceDetailRequestHeader(config, row);
                InvoiceDetailOrder invDetOrder = GetInvoiceDetailOrder(row);
                InvoiceDetailSummary invDetSummary = GetInvoiceDetailSummary(row);

                InvoiceDetailRequest invDetReq = new InvoiceDetailRequest 
                { 
                    InvoiceDetailRequestHeader = invDetReqHdr,
                    InvoiceDetailSummary = invDetSummary,
                };

                object[] invDetReqItems = new object[] { invDetOrder };
                invDetReq.Items = invDetReqItems;

                invRequest.Item = invDetReq;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return invRequest;
        }

        private static InvoiceDetailRequestHeader GetInvoiceDetailRequestHeader(CXmlInvoiceGeneratorConfiguration config, DataRow row)
        {
            //create and return the CXml InvoiceDetailRequestHeader
            Console.WriteLine("Get the CXml RequestHeader for Invoice " + row["Id"].ToString());

            InvoiceDetailRequestHeader invDetReqHdr = new InvoiceDetailRequestHeader
            {
                invoiceID = row["Id"].ToString(),
                invoiceDate = DateTime.Now.ToString(),
                Items = new object[]
                    {
                        new InvoiceDetailPaymentTerm
                        {
                             payInNumberOfDays = row["PaymentTermsDays"].ToString()
                        }
                    }
            };

            try
            {
                switch (config.Purpose)
                {
                    case "standard":
                        invDetReqHdr.purpose = InvoiceDetailRequestHeaderPurpose.standard;
                        break;
                    case "creditMemo":
                        invDetReqHdr.purpose = InvoiceDetailRequestHeaderPurpose.creditMemo;
                        break;
                    case "debitMemo":
                        invDetReqHdr.purpose = InvoiceDetailRequestHeaderPurpose.debitMemo;
                        break;
                    case "lineLevelCreditMemo":
                        invDetReqHdr.purpose = InvoiceDetailRequestHeaderPurpose.lineLevelCreditMemo;
                        break;
                    case "lineLevelDebitMemo":
                        invDetReqHdr.purpose = InvoiceDetailRequestHeaderPurpose.lineLevelDebitMemo;
                        break;
                }

                switch (config.Purpose)
                {
                    case "new":
                        invDetReqHdr.operation = InvoiceDetailRequestHeaderOperation.@new;
                        break;
                    case "delete":
                        invDetReqHdr.operation = InvoiceDetailRequestHeaderOperation.delete;
                        break;
                }

                //add the InvoicePartners
                InvoicePartner invSoldTo = new InvoicePartner();
                string? salesOrderId = row["SalesOrderId"].ToString();
                if (int.TryParse(salesOrderId, out int intSalesOrderId))
                {
                    invSoldTo = GetInvoicePartner(ContactRole.cRole_soldTo, intSalesOrderId);
                }

                InvoicePartner invBillTo = new InvoicePartner();
                string? invoiceId = row["Id"].ToString();
                if (int.TryParse(invoiceId, out int intId))
                {
                    invBillTo = GetInvoicePartner(ContactRole.cRole_billTo, intId);
                }

                InvoicePartner[] invPartners = new InvoicePartner[] { invSoldTo, invBillTo };
                invDetReqHdr.InvoicePartner = invPartners;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return invDetReqHdr;
        }

        private static InvoicePartner GetInvoicePartner(ContactRole role, int Id)
        {
            //create and return the CXml InvoicePartner
            InvoicePartner partner = new InvoicePartner();
            DataRow addressData;

            try
            {
                Invoices invoiceDB = new();

                switch (role)
                {
                    case ContactRole.cRole_soldTo:
                        Console.WriteLine("Get the CXml SoldTo Partner for SalesOrder " + Id.ToString());
                        addressData = invoiceDB.GetDeliveryAddressForSalesOrder(Id);
                        break;

                    case ContactRole.cRole_billTo:
                        Console.WriteLine("Get the CXml BillTo Partner for Invoice " + Id.ToString());
                        addressData = invoiceDB.GetBillingAddressForInvoice(Id);
                        break;

                    default:
                        throw new Exception("GetInvoicePartner - invalid role");
                }

                Name name = new Name
                {
                    Value = addressData["ContactName"].ToString()
                };

                PostalAddress address = new PostalAddress
                {
                    City = new City
                    {
                         Value = addressData["AddrLine3"].ToString()
                    },
                    State = new State
                    {
                        Value = addressData["AddrLine4"].ToString()
                    },
                    PostalCode = addressData["AddrPostCode"].ToString(),
                    Country = new Country
                    {
                         isoCountryCode = addressData["CountryCode"].ToString(),
                         Value = addressData["Country"].ToString()
                    }
                };

                string? addr1 = addressData["AddrLine1"].ToString();
                string? addr2 = addressData["AddrLine2"].ToString();
                int lineCount = (addr1 == null ? 0 : 1) + (addr2 == null ? 0 : 1);
                if (lineCount > 0)
                {
                    int currentLine = 0;
                    string[] streetAddr = new string[lineCount];

                    if (addr1 != null)
                    {
                        streetAddr[currentLine] = addr1;
                        currentLine++;
                    }
                    if (addr2 != null)
                    {
                        streetAddr[currentLine] = addr2;
                    }

                    address.Street = streetAddr;
                }

                partner.Contact = new Contact 
                { 
                    role = ContactRoleDescription(role) ,
                    Name = name,
                    PostalAddress = new PostalAddress[] { address }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return partner;
        }

        private static string ContactRoleDescription(ContactRole role)
        {
            //return the description for the given ContactRole
            string desc = string.Empty;

            try
            {
                switch (role)
                {
                    case ContactRole.cRole_soldTo:
                        desc = "soldTo";
                        break;

                    case ContactRole.cRole_billTo:
                        desc = "billTo";
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return desc;
        }

        private static InvoiceDetailOrder GetInvoiceDetailOrder(DataRow row)
        {
            //create and return the CXml InvoiceDetailOrder
            Console.WriteLine("Get the CXml InvoiceDetailOrder for Invoice " + row["Id"].ToString());

            InvoiceDetailOrder invDetOrder = new InvoiceDetailOrder
            {
                InvoiceDetailOrderInfo = new InvoiceDetailOrderInfo
                {
                    Items = new object[] 
                    { 
                        new OrderReference
                        { 
                            DocumentReference = new DocumentReference
                            {
                                payloadID=row["SalesOrderId"].ToString()
                            }
                        }
                    }
                }
            };

            try
            {
                string? invoiceId = row["Id"].ToString();
                if (int.TryParse(invoiceId, out int intInvoiceId))
                {
                    Invoices invoiceDB = new();
                    DataTable invoiceItems = invoiceDB.GetItemsOnInvoice(intInvoiceId);

                    object[] items = new object[invoiceItems.Rows.Count];
                
                    //loop through each 'Order Item' row
                    int itemIndex = 0;
                    foreach (DataRow itemRow in invoiceItems.Rows)
                    {
                        items[itemIndex] = GetInvoiceDetailOrderItem(row, itemRow);
                        itemIndex++;
                    }

                    invDetOrder.Items = items;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return invDetOrder;
        }

        private static InvoiceDetailItem GetInvoiceDetailOrderItem(DataRow row, DataRow itemRow)
        {
            //create and return the CXml InvoiceDetailItem
            Console.WriteLine("Get the CXml InvoiceDetailOrder for Invoice " + row["Id"].ToString() + " for Item " + itemRow["Id"].ToString());

            InvoiceDetailItem invDetItem = new InvoiceDetailItem 
            {
                quantity = itemRow["Qty"].ToString(),
                UnitPrice = new UnitPrice
                {
                    Money = new Money
                    {
                        currency = row["CurrencyCode"].ToString(),
                        Value = itemRow["UnitPrice"].ToString()
                    }
                },
                InvoiceDetailItemReference = new InvoiceDetailItemReference
                {
                    ItemID = new ItemID
                    {
                        SupplierPartID = new SupplierPartID
                        {
                             Value = itemRow["StockItemId"].ToString()
                        }
                    },
                    ManufacturerPartID = itemRow["PartNo"].ToString(),
                    ManufacturerName = new ManufacturerName
                    {
                        Value = itemRow["Manufacturer"].ToString(),
                    }
                },
                SubtotalAmount = new SubtotalAmount
                {
                    Money = new Money
                    {
                        currency = row["CurrencyCode"].ToString(),
                        Value = itemRow["LineTotal"].ToString()
                    }
                },
                GrossAmount = new GrossAmount
                {
                    Money = new Money
                    {
                        currency = row["CurrencyCode"].ToString(),
                        Value = itemRow["LineTotal"].ToString()
                    }
                },
                NetAmount = new NetAmount   //nb. I've NOT calculated the tax amout per item. Could do if needed.
                {
                    Money = new Money
                    {
                        currency = row["CurrencyCode"].ToString(),
                        Value = itemRow["LineTotal"].ToString()
                    }
                }
            };

            try
            {
                string? desc = itemRow["Description"].ToString();
                if (desc != null)
                {
                    invDetItem.InvoiceDetailItemReference.Description = new Description
                    {
                        Text = new string[] { desc }
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return invDetItem;
        }

        private static InvoiceDetailSummary GetInvoiceDetailSummary(DataRow row)
        {
            //create and return the CXml InvoiceDetailSummary
            Console.WriteLine("Get the CXml InvoiceDetailSummary for Invoice " + row["Id"].ToString());

            InvoiceDetailSummary invDetSummary = new InvoiceDetailSummary
            {
                Tax = new Tax
                {
                    Money = new Money
                    {
                        currency = row["CurrencyCode"].ToString()
                    }       
                },
                GrossAmount = new GrossAmount
                {
                    Money = new Money
                    {
                        currency = row["CurrencyCode"].ToString(),
                        Value = row["GrossAmount"].ToString()
                    }
                },
                NetAmount = new NetAmount
                {
                    Money = new Money
                    {
                        currency = row["CurrencyCode"].ToString(),
                        Value = row["NetAmount"].ToString()
                    }
                }
            };

            try
            {
                TaxDetail taxDtl = new TaxDetail
                {
                    purpose = "tax",
                    category = row["VATCode"].ToString(),
                    percentageRate = row["VATPercentage"].ToString(),
                    TaxableAmount = new TaxableAmount
                    {
                        Money = new Money
                        {
                            currency = row["CurrencyCode"].ToString(),
                            Value = row["GrossAmount"].ToString()
                        }
                    },
                    TaxAmount = new TaxAmount
                    {
                        Money = new Money
                        {
                            currency = row["CurrencyCode"].ToString(),
                            Value = row["VATAmount"].ToString()
                        }
                    }
                };

                invDetSummary.Tax.TaxDetail = new TaxDetail[] { taxDtl };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return invDetSummary;
        }

    }
}
