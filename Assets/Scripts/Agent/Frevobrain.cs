using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

// One FREVO FullyMeshedNet. Give EVERY game agent its own instance (the net is recurrent -> keeps state).
public class FrevoBrain
{
    static readonly CultureInfo CI = CultureInfo.InvariantCulture; // .zre uses '.' decimals

    public readonly int Inputs, Outputs, NodeCount;
    public readonly float Fitness;
    readonly int iterations;
    readonly float[] bias;
    readonly float[,] w;      // w[to, from]
    float[] act;              // node activations, persist between ticks

    // Loads the highest-fitness candidate from the .zre text. iterations = value from the .zre (<representationentry key="iterations">)
    public static FrevoBrain LoadBest(string zreText, int iterations = 2)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null }; // skip the C:\...ISave.dtd reference
        XDocument doc;
        using (var reader = XmlReader.Create(new StringReader(zreText), settings))
            doc = XDocument.Load(reader);

        var best = doc.Descendants("FullyMeshedNet")
                      .OrderByDescending(n => float.Parse(n.Attribute("fitness").Value, CI))
                      .First();
        return new FrevoBrain(best, iterations);
    }

    FrevoBrain(XElement net, int iterations)
    {
        this.iterations = iterations;
        Inputs = int.Parse(net.Attribute("input_nodes").Value);
        Outputs = int.Parse(net.Attribute("output_nodes").Value);
        NodeCount = int.Parse(net.Attribute("nodes").Value);
        Fitness = float.Parse(net.Attribute("fitness").Value, CI);

        bias = new float[NodeCount];
        w = new float[NodeCount, NodeCount];
        act = new float[NodeCount];

        foreach (var node in net.Descendants("node").Where(n => (string)n.Attribute("type") == "node"))
        {
            int to = int.Parse(node.Attribute("nr").Value);
            bias[to] = float.Parse(node.Element("bias").Value, CI);
            foreach (var wt in node.Descendants("weight"))
                w[to, int.Parse(wt.Attribute("from").Value)] = float.Parse(wt.Value, CI);
        }
    }

    public void Reset() => act = new float[NodeCount];

    // inputs must be in the SAME order/scale as your Java sensor code
    public float[] Evaluate(float[] input)
    {
        for (int i = 0; i < Inputs; i++) act[i] = input[i];

        for (int it = 0; it < iterations; it++)
        {
            var next = (float[])act.Clone();              // synchronous update (verify in FullyMeshedNet.java)
            for (int j = Inputs; j < NodeCount; j++)
            {
                float s = bias[j];
                for (int k = 0; k < NodeCount; k++) s += w[j, k] * act[k];
                next[j] = s;                               // LINEAR activation
            }
            act = next;
        }

        // ASSUMPTION: outputs are the last nodes (12..18 here). Confirm against FullyMeshedNet.java.
        var output = new float[Outputs];
        for (int o = 0; o < Outputs; o++) output[o] = act[NodeCount - Outputs + o];
        return output;
    }
}