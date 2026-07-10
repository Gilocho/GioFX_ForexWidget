namespace ForexWidget.Infrastructure.Security;

using System.IO;
using System.Xml;
using System.Xml.Linq;

/// <summary>
/// Parseo XML endurecido para contenido descargado de internet.
/// Bloquea XXE (entidades externas) y "billion laughs" de forma explícita —
/// no se depende de los defaults implícitos de la librería, que pueden
/// cambiar entre versiones o métodos de parseo.
/// </summary>
public static class SafeXml
{
    /// <summary>
    /// Parses untrusted XML. Throws XmlException on any DOCTYPE/DTD
    /// (DtdProcessing.Prohibit) instead of resolving entities.
    /// </summary>
    public static XDocument Parse(string xmlContent)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,   // bloquea DOCTYPE/DTD por completo
            XmlResolver = null,                       // sin resolución de entidades externas
            MaxCharactersFromEntities = 1024,         // límite defensivo adicional
        };

        using var stringReader = new StringReader(xmlContent);
        using var xmlReader = XmlReader.Create(stringReader, settings);
        return XDocument.Load(xmlReader);
    }
}
