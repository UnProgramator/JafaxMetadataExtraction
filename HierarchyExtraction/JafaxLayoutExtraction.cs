using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Globalization;

namespace DRSTool.Extractor.DataExtraction;

class JafaxLayoutExtraction
{
    public void extract(string inputFileName, string outputFileName, string? prefix=null)
    {
        var input = getArrayContent<Dictionary<string, object>>(inputFileName);

        if (input == null)
            throw new Exception("Read or parse error or file Empty");

        var clsLst = input.Where(checkType).Where(checkPublicClass).Where(checkHasName).ToList();

        Dictionary<long, object> output = new Dictionary<long, object>();
        List<DummyInterfaceMode> classInfo = new List<DummyInterfaceMode>();

        foreach(var cls in clsLst)
        {
            //if (!cls["type"].Equals("Class")) continue;

            if (!cls.ContainsKey("fileName"))
            {
                Console.WriteLine($"invalid id {cls["id"]} doesn't contain a file Name for class {cls["name"]}, {(cls.ContainsKey("isInterface") ? "is interface" : "" )}");
                continue;
            }

            var id = (long)cls["id"];
            string fileName = (string)cls["fileName"];

            if(prefix is not null)
            {
                if(fileName.StartsWith(prefix))
                    fileName = fileName.Substring(prefix.Length);
            }

            output.Add(id, fileName);

            long methodNum = cls.ContainsKey("containedMethods") ? ((ICollection)cls["containedMethods"]).Count : 0;

            classInfo.Add(new DummyInterfaceMode(  fileName, 
                                                    cls.ContainsKey("isInterface"),
                                                    checkAbstractClass(cls),
                                                    methodNum));
        }

        List<OutInheritance> files = new List<OutInheritance>();

        foreach (var cls in clsLst)
        {
            if (!output.ContainsKey((long)cls["id"])) continue;
            string _derived = (string)output[(long)cls["id"]];

            //if (!cls["type"].Equals("Class")) continue;

            if (cls.ContainsKey("superClass"))
            {
                if (output.ContainsKey((long)cls["superClass"]))
                {
                    string _base = (string)output[(long)cls["superClass"]];
                    files.Add(new OutInheritance(_base, _derived));
                }
                else
                    Console.WriteLine($"class {_derived} inherits an invalid class {cls["superClass"]}");
            }
            

            if (cls.ContainsKey("interfaces"))
            {
                foreach(var intId in (IEnumerable)cls["interfaces"])
                {
                    long id = (long)((JValue)intId).Value;
                    if (!output.ContainsKey(id))
                    {
                        Console.WriteLine($"class {_derived} inherits an invalid class {id}");
                        continue;
                    }
                    string _base = (string)output[id];
                    files.Add(new OutInheritance(_base, _derived));
                }
            }
        }

        writeContent(outputFileName + "inheritance.csv", files);
        writeContent(outputFileName + "class-info.csv", classInfo);

    }

    private static bool checkType(Dictionary<string, object> x)
    {
        if (!x.ContainsKey("type") || x["type"] is null)
            return false;
        return x["type"].Equals("Class") && x.ContainsKey("fileName") && x.ContainsKey("name");
    }

    private static bool checkName(Dictionary<string, object> x)
    {
        string name = (string)x["name"];
        string fileName = (string)x["fileName"];

        fileName = fileName.Substring(fileName.LastIndexOf('/')+1, fileName.LastIndexOf('.') - (fileName.LastIndexOf('/') + 1));
        return name.Equals(fileName);
    }

    private static bool checkPublicClass(Dictionary<string, object> x)
    {
        if (x.ContainsKey("container")) // ignore imbricated classes
            return false;

        if (!x.ContainsKey("modifiers")) // modifier private is for private. for public, either public or no modifiers at all
            return checkName(x);

        foreach (var mod in (IEnumerable)x["modifiers"])
        {
            if (((string)((JValue)mod).Value).Equals("Public"))
                return true;
        }

        return checkName(x);
    }

    private static bool checkAbstractClass(Dictionary<string, object> x)
    {
        if (!x.ContainsKey("modifiers")) // guard
            return false;

        foreach (var mod in (IEnumerable)x["modifiers"])
        {
            if (((string)((JValue)mod).Value).Equals("Abstract"))
                return true;
        }

        return false;
    }

    private static bool checkHasName(Dictionary<string, object> x)
    {
        return x.ContainsKey("name") && ((string)x["name"]).Length > 0;
    }

    private IEnumerable<T>? getArrayContent<T>(string filename)
    {
        IEnumerable<T>? content = null;
        var _file = File.OpenText(filename);
        JsonSerializer serializer = new JsonSerializer();
        content = (IEnumerable<T>?)serializer.Deserialize(_file, typeof(IEnumerable<T>));

        return content;
    }

    public void writeContent(string filename, object content)
    {
        using (var writer = new StreamWriter(filename))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            if (content is IEnumerable)
                csv.WriteRecords((IEnumerable)content);
            else
                csv.WriteRecord(content);
        }
    }
}

internal class OutInheritance
{
    public string Base { get; private set; }
    public string Derived { get; private set; }

    public OutInheritance(string _base, string _derived)
    {
        Base = _base;
        Derived = _derived;
    }
}

internal class DummyInterfaceMode
{
    
    public string file { get; }
    public bool isInterfaces { get; }
    public bool isAbstract { get; }
    public long methodNum { get; }

    public DummyInterfaceMode(string file, bool isInterfaces, bool isAbstract, long methodNum)
    {
        this.isInterfaces = isInterfaces;
        this.isAbstract = isAbstract;
        this.file = file;
        this.methodNum = methodNum;
    }
}


