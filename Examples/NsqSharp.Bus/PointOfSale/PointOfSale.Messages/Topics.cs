using System;
using System.Collections.Generic;
using System.Linq;
using PointOfSale.Messages.Customers;
using PointOfSale.Messages.Invoices;
using PointOfSale.Messages.Products;

namespace PointOfSale.Messages
{
    public class Topics
    {
        private readonly Dictionary<Type, string> _typeTopics;

        public Topics()
        {
            _typeTopics = new Dictionary<Type, string>();

            // Customers
            Add<GetCustomerDetails>("pos.customer.get-details");
            Add<GetCustomers>("pos.customer.get-all");

            // Invoices
            Add<GetInvoiceDetails>("pos.invoice.get-details");
            Add<GetInvoices>("pos.invoice.get-all");
            Add<GetInvoiceSummary>("pos.invoice.get-summary");

            // Products
            Add<GetProductDetails>("pos.products.get-details");
            Add<GetProducts>("pos.products.get-all");

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
