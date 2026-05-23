using System; using System.Reflection; using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
class Check {
    static void Main(string[] args) {
        var asm = Assembly.LoadFrom(args[0]);
        var classes = asm.GetTypes().Where(t => t.GetCustomAttributes(typeof(TestClassAttribute), false).Any()).ToList();
        Console.WriteLine("TestClass count: " + classes.Count);
        foreach (var c in classes.OrderBy(c => c.Name)) Console.WriteLine("  " + c.Name);
    }
}
