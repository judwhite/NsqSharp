using System;
using System.Collections.Generic;
using System.Linq;
using PointOfSale.Messages.Customers.Commands;
using PointOfSale.Messages.Customers.Events;
using PointOfSale.Messages.Invoices.Commands;
using PointOfSale.Messages.Invoices.Events;
using PointOfSale.Messages.Products.Commands;
using PointOfSale.Messages.Products.Events;

namespace PointOfSale.Messages
{
    public class Topics
    {
        private readonly Dictionary<Type, string> _typeTopics;

        public Topics()
        {
            _typeTopics = new Dictionary<Type, string>();

            // Customers
            Add<GetCustomersCommand>("pos.customer.cmd.get-all");
            Add<CustomerIdFoundEvent>("pos.customer.evnt.customerid-found");

            // Invoices
            Add<GetInvoicesCommand>("pos.invoice.cmd.get-all");
            Add<InvoiceIdFoundEvent>("pos.invoice.evnt.invoiceid-found");

            // Products
            Add<GetProductsCommand>("pos.products.cmd.get-all");
            Add<ProductIdFoundEvent>("pos.products.evnt.productid-found");

            Validate();
        }

        public string GetTopic(Type messageType)
        {
            return _typeTopics[messageType];
        }

        private void Add<T>(string topicName)
        {
            _typeTopics.Add(typeof(T), topicName);
        }

        private void Validate()
        {
            // Check for duplicate topic names
            var dupes = _typeTopics
                            .GroupBy(p => p.Value.ToLower())
                            .Where(g => g.Count() > 1)
                            .Select(p => p.Key)
                            .ToList();

            if (dupes.Count != 0)
            {
                throw new Exception(string.Format("Duplicate topic name(s): {0}",
                    string.Join(", ", dupes)));
            }

            // Check for missed types
            var missingTypes = new List<Type>();
            foreach (var messageType in typeof(Topics).Assembly.GetTypes())
            {
                if (messageType == typeof(Topics))
                    continue;

                if (!_typeTopics.ContainsKey(messageType))
                    missingTypes.Add(messageType);
            }

            if (missingTypes.Count != 0)
            {
                throw new Exception(string.Format("Type(s) missing topic: {0}",
                    string.Join(", ", missingTypes.Select(p => p.Name))));
            }
        }
    }
}
