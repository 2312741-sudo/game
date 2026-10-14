using System.Text;

namespace TramChanh.Core
{
    /// <summary>
    /// Attribution text for assets licensed CC-BY (the licence requires visible credit). Keep in step with
    /// Docs/Reference/ASSET_LICENSES.md: every CC-BY row there needs an entry here.
    /// </summary>
    public static class CreditsText
    {
        public readonly struct Entry
        {
            public Entry(string title, string author, string license, string source)
            {
                Title = title; Author = author; License = license; Source = source;
            }
            public string Title { get; }
            public string Author { get; }
            public string License { get; }
            public string Source { get; }
        }

        public static readonly Entry[] Entries =
        {
            new Entry("Metal Sheet Roof \"New\" & \"Old\" | Part-1", "CHEESE ARG 3D", "CC-BY 4.0", "sketchfab.com/3d-models/metal-sheet-roof-new-old-part-1-c7c46bf23a7e41068ab302cbc8152046"),
            new Entry("Table & Chair Plastic (Ban Ghe Nhua Viet Nam), reduced triangle count", "minchou19", "CC-BY 4.0", "sketchfab.com/3d-models/table-chair-plastic-ban-ghe-nhua-viet-nam-8216465bc93142f89f7cd38446be6432"),
            new Entry("Blue/White Beach Umbrella", "S. Paul Michael", "CC-BY 3.0", "poly.pizza/m/1BLLDjRdxoa"),
            new Entry("Urban Commuter Scooter (CUPIC_SCOOTER)", "Poly by Google", "CC-BY 3.0", "poly.pizza/m/9RqkkBOPNtd"),
            new Entry("Stool", "Poly by Google", "CC-BY 3.0", "poly.pizza/m/2UeLZTX5nHr"),
            new Entry("Birds of paradise potted plant", "Poly by Google", "CC-BY 3.0", "poly.pizza/m/folDA1yLeEF"),
            new Entry("Dining car", "Poly by Google", "CC-BY 3.0", "poly.pizza/m/5teci4b6krG"),
            new Entry("Canopy", "Poly by Google", "CC-BY 3.0", "poly.pizza/m/40tUFNg2fu4"),
            new Entry("Bicycle", "Poly by Google", "CC-BY 3.0", "poly.pizza/m/b6USSQA731J"),
            new Entry("Motorcycle", "Poly by Google", "CC-BY 3.0", "poly.pizza/m/5_MTCnqfUTr"),
        };

        public const string Footer = "CC0 models: Quaternius, Kay Lousberg, BlenderVoyage (no credit required, thank you).";

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.Append("CREDITS\n\n");
            foreach (Entry e in Entries)
            {
                sb.Append(e.Title).Append(" by ").Append(e.Author).Append(" (").Append(e.License).Append(")\n");
                sb.Append("  ").Append(e.Source).Append('\n');
            }
            sb.Append('\n').Append(Footer);
            return sb.ToString();
        }
    }
}
