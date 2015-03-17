using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using PointOfSale.Common.Config;
using PointOfSale.Common.Utils;
using PointOfSale.Services.Invoices.Models;

namespace PointOfSale.Services.Invoices
{
    public class InvoiceService : IInvoiceService
    {
        private readonly string _invoiceEndpoint;
        private readonly string _invoiceDetailEndpoint;
        private readonly IRestClient _restClient;

        public InvoiceService(IServiceEndpoints serviceEndpoints, IRestClient restClient)
        {
            if (serviceEndpoints == null)
                throw new ArgumentNullException("serviceEndpoints");
            if (restClient == null)
                throw new ArgumentNullException("restClient");

            _invoiceEndpoint = serviceEndpoints.InvoiceEndpoint;
            _invoiceDetailEndpoint = serviceEndpoints.InvoiceDetailsEndpoint;
            _restClient = restClient;
        }

        public Collection<int> GetInvoiceIds()
        {
            string response = _restClient.Get(_invoiceEndpoint);

            var invoiceIds = XDocument.Parse(response).Root.Elements("INVOICE").Select(p => (int)p).ToList();

            return new Collection<int>(invoiceIds);
        }

        public InvoiceSummary GetInvoiceSummary(int invoiceId)
        {
            string response = _restClient.Get(string.Format("{0}/{1}", _invoiceEndpoint, invoiceId));

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
            string response = _restClient.Get(string.Format("{0}/{1}", _invoiceDetailEndpoint, invoiceId));

            var xml = XDocument.Parse(response).Root;

            var list = new Collection<InvoiceDetail>();
            foreach (var lineItem in xml.Elements("INVOICEID"))
            {
                var invoiceDetail = new InvoiceDetail { InvoiceId = (int)lineItem };

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

                    element = (XElement)element.NextNode;
                }

                list.Add(invoiceDetail);
            }

            return list;
        }
    }
}
