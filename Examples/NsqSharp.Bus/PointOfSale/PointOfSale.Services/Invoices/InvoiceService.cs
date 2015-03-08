using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using PointOfSale.Services.Invoices.Models;

namespace PointOfSale.Services.Invoices
{
    public class InvoiceService : IInvoiceService
    {
        private readonly string _invoiceEndpoint;
        private readonly string _invoiceDetailEndpoint;

        public InvoiceService(string invoiceEndpoint, string invoiceDetailsEndpoint)
        {
            if (string.IsNullOrEmpty(invoiceEndpoint))
                throw new ArgumentNullException("invoiceEndpoint");
            if (string.IsNullOrEmpty(invoiceDetailsEndpoint))
                throw new ArgumentNullException("invoiceDetailsEndpoint");

            _invoiceEndpoint = invoiceEndpoint;
            _invoiceDetailEndpoint = invoiceDetailsEndpoint;
        }

        public Collection<int> GetInvoiceIds()
        {
            var webClient = new WebClient();
            string response = webClient.DownloadString(_invoiceEndpoint);

            var invoiceIds = XDocument.Parse(response).Root.Elements("INVOICE").Select(p => (int)p).ToList();

            return new Collection<int>(invoiceIds);
        }

        public InvoiceSummary GetInvoiceSummary(int invoiceId)
        {
            var webClient = new WebClient();
            string response = webClient.DownloadString(string.Format("{0}/{1}", _invoiceEndpoint, invoiceId));

            var xml = XDocument.Parse(response).Root;

            return new InvoiceSummary
            {
                InvoiceId = (int)xml.Element("ID"),
                CustomerId = (int)xml.Element("CUSTOMERID"),
                Total = (decimal)xml.Element("TOTAL")
            };
        }

        public Collection<InvoiceDetail> GetInvoiceDetails(int invoiceId)
        {
            var webClient = new WebClient();
            string response = webClient.DownloadString(string.Format("{0}/{1}", _invoiceDetailEndpoint, invoiceId));

            var xml = XDocument.Parse(response).Root;

            var list = new Collection<InvoiceDetail>();
            foreach (var lineItem in xml.Elements("INVOICEID"))
            {
                var invoiceDetail = new InvoiceDetail();

                var element = (XElement)lineItem.NextNode;
                while (element != null && element.Name != "INVOICEID")
                {
                    switch (element.Name.LocalName)
                    {
                        case "ITEM":
                            invoiceDetail.ItemIndex = (int)element;
                            break;
                        case "PRODUCTID":
                            invoiceDetail.ProductId = (int)element;
                            break;
                        case "QUANTITY":
                            invoiceDetail.Quantity = (int)element;
                            break;
                        case "COST":
                            invoiceDetail.Cost = (decimal)element;
                            break;
                    }
                }

                list.Add(invoiceDetail);
            }

            return list;
        }
    }
}
