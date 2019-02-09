using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Calas.Scrape.Attributes;
using HtmlAgilityPack;

namespace Calas.Scrape.Scrapers
{
    public class TableScraper<TModel>
        where TModel : class, new()
    {
        private readonly ICollection<(string title, MethodInfo setter)> _tableSetters;
        private readonly ICollection<(string name, MethodInfo setter)> _formSetters;

        private ICollection<(int index, string title, List<MethodInfo> setters)> _indexedTableSetters;
        private readonly object _lock = new object();

        public TableScraper()
        {
            var properties = typeof(TModel).GetProperties()
                                           .Select(x => (prop: x, attr: x.GetCustomAttribute<DataFieldAttribute>()))
                                           .Where(x => x.attr != null)
                                           .Select(x => (type: x.attr.Type, title: x.attr.Name.ToLower().Trim(), setter: x.prop.GetSetMethod()))
                                           .ToList();

            _tableSetters = properties.Where(x => x.type == DataFieldType.Table).Select(x => (x.title, x.setter)).ToList();
            _formSetters = properties.Where(x => x.type == DataFieldType.Form).Select(x => (name: x.title, x.setter)).ToList();
        }

        public IEnumerable<TModel> ScrapeTable(HtmlNode html)
        {
            if (!_tableSetters.Any())
            {
                throw new ArgumentException("No data table properties found on type: " + typeof(TModel));
            }

            if (html.SelectSingleNode(".//tr") == null)
            {
                // No rows.
                yield break;
            }

            EnsureIndexedTableSetters(html);

            var rows = html.SelectNodes(".//tbody//tr") ?? html.SelectNodes(".//tr");

            foreach (var row in rows)
            {
                var model = new TModel();
                var setters = row.SelectNodes(".//td")
                                 .Select((d, i) => (index: i, data: d.InnerText.Trim()))
                                 .Where(x => !string.IsNullOrEmpty(x.data) && x.data != "Not set")
                                 .Join(_indexedTableSetters,
                                       d => d.index,
                                       x => x.index,
                                       (d, x) => x.setters.Select(s => (setter: s, d.data)))
                                 .SelectMany(x => x);

                foreach (var (setter, data) in setters)
                {
                    setter.Invoke(model, new object[] { data });
                }

                yield return model;
            }
        }

        public void ScrapeForm(HtmlDocument html, string formName, TModel model)
        {
            if (!_formSetters.Any())
            {
                throw new ArgumentException("No data form properties found on type: " + typeof(TModel));
            }

            var form = html.DocumentNode.SelectNodes("//form[@action='/run.php']")
                           .FirstOrDefault(x => x.SelectNodes($"input[@type='hidden' and @name='wfw_form_id' and @value='{formName}']").Any());

            if (form == null)
            {
                throw new InvalidOperationException("Cannot find form: " + formName);
            }

            foreach (var (name, setter) in _formSetters)
            {
                var input = form.SelectSingleNode($".//input[@name='{name}']");
                if (input == null)
                {
                    throw new InvalidOperationException("Cannot find form field: " + name);
                }

                var value = input.GetAttributeValue("value", null);
                if (!string.IsNullOrEmpty(value) && value != "Not set")
                {
                    setter.Invoke(model, new object[] { value });
                }
            }
        }

        private void EnsureIndexedTableSetters(HtmlNode html)
        {
            if (_indexedTableSetters != null)
            {
                return;
            }

            lock (_lock)
            {
                if (_indexedTableSetters != null)
                {
                    return;
                }

                var tableHeaders = html.SelectNodes(".//thead//th");
                if (tableHeaders == null || !tableHeaders.Any())
                {
                    throw new InvalidOperationException("Not table header and no indexed table setters configured");
                }

                _indexedTableSetters = tableHeaders.Select((n, i) => (index: i, title: n.InnerText))
                                                   .GroupJoin(_tableSetters,
                                                              x => x.title.ToLower().Trim(),
                                                              y => y.title,
                                                              (x, ys) => (x.index, x.title, setters: ys.Select(y => y.setter).ToList()))
                                                   .Where(x => x.setters.Any())
                                                   .ToList();
                if (_indexedTableSetters.Sum(x => x.setters.Count) != _tableSetters.Count)
                {
                    var unmatchedSetters = _tableSetters.Where(s => _indexedTableSetters.All(x => x.title.ToLower().Trim() != s.title))
                                                        .Select(x => x.title)
                                                        .ToList();
                    throw new InvalidOperationException("Not all setters matched: " + string.Join(", ", unmatchedSetters));
                }
            }
        }
    }
}
