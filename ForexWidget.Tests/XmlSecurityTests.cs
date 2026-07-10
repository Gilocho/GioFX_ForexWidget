namespace ForexWidget.Tests;

using ForexWidget.Infrastructure.Security;
using System.Diagnostics;
using System.Text;
using System.Xml;
using Xunit;

public class XmlSecurityTests
{
    [Fact]
    public void Case1_XxeWithExternalFileEntity_IsRejectedWithoutLeakingFileContent()
    {
        const string xxePayload = """
            <?xml version="1.0"?>
            <!DOCTYPE foo [<!ENTITY xxe SYSTEM "file:///C:/Windows/win.ini">]>
            <weeklyevents><event><title>&xxe;</title></event></weeklyevents>
            """;

        // DtdProcessing.Prohibit hace fallar el parseo al encontrar el DOCTYPE,
        // así que el contenido del archivo local jamás puede llegar al resultado.
        var ex = Assert.Throws<XmlException>(() => SafeXml.Parse(xxePayload));
        Assert.DoesNotContain("win.ini", ex.Message);
    }

    [Fact]
    public void Case2_NormalXmlWithoutDoctype_ParsesUnchanged()
    {
        const string validXml = """
            <?xml version="1.0"?>
            <weeklyevents>
                <event>
                    <title>Non-Farm Payrolls</title>
                    <country>USD</country>
                </event>
            </weeklyevents>
            """;

        var doc = SafeXml.Parse(validXml);

        Assert.Equal("Non-Farm Payrolls", doc.Root?.Element("event")?.Element("title")?.Value);
    }

    [Fact]
    public void Case3_BillionLaughsPayload_FailsFastWithoutExpandingEntities()
    {
        // Entidades anidadas que expandirían exponencialmente si se procesaran
        var payload = new StringBuilder();
        payload.AppendLine("""<?xml version="1.0"?>""");
        payload.AppendLine("<!DOCTYPE lolz [");
        payload.AppendLine("""  <!ENTITY lol "lolololololololololol">""");
        for (int i = 1; i <= 9; i++)
            payload.AppendLine($"""  <!ENTITY lol{i} "&lol{i - 1};&lol{i - 1};&lol{i - 1};&lol{i - 1};&lol{i - 1};&lol{i - 1};&lol{i - 1};&lol{i - 1};&lol{i - 1};&lol{i - 1};">""".Replace("lol0", "lol"));
        payload.AppendLine("]>");
        payload.AppendLine("<weeklyevents>&lol9;</weeklyevents>");

        var sw = Stopwatch.StartNew();
        Assert.Throws<XmlException>(() => SafeXml.Parse(payload.ToString()));
        sw.Stop();

        // Prohibit rechaza el DOCTYPE antes de expandir nada: debe fallar en
        // milisegundos, no colgarse consumiendo memoria.
        Assert.True(sw.ElapsedMilliseconds < 2000,
            $"El rechazo tardó {sw.ElapsedMilliseconds}ms — debería ser inmediato");
    }
}
