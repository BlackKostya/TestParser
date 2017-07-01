using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestParser
{
    internal class Cell : IEnumerable<Cell>
    {
        private string _value;
        private List<Cell> _children = null;
        public string Name { get; set; }
        public Cell Parent { get; private set; } = null;
        public long Id { get; private set; }
        public bool isList => _children == null;
        public string Content
        {
            get
            {
                return _value;
            }
            set
            {
                _children?.Clear();
                _children = null;
                _value = value;
            }
        }

        public Cell(Cell parent, long id)
        {
            Parent = parent;
            Id = id;
        }

        public void AddChild(Cell child)
        {
            _value = null;
            if (_children == null)
                _children = new List<Cell>();
            _children.Add(child);
        }

        public IEnumerator<Cell> GetEnumerator()
        {
            return _children?.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _children?.GetEnumerator();
        }
        public override string ToString()
        {
            StringBuilder s = new StringBuilder(string.Format("{0} {1} {2}:", Id, Parent == null ? "" : Parent.Id.ToString(), Name));
            if (isList)
            {
                s.Append(Content);
            }
            else
            { 
                foreach (Cell c in this)
                    s.Append(string.Format("'{0}' ", c.Id));
                s.AppendLine();
                foreach (Cell c in this)
                    s.AppendLine(c.ToString());
            }
            return s.ToString();
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() != 2) return;
            try
            {
                List<Cell> list = Parser(args[0]);
                Stack<Cell> stack = new Stack<Cell>();
                FileInfo fo = new FileInfo(args[1]);
                using (FileStream fstream = fo.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (var writer = new StreamWriter(fstream, Encoding.UTF8))
                    {
                        foreach (Cell cell in list)
                        {
                            Console.WriteLine(cell);
                            writer.WriteLine(cell);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.Read();
        }


        private enum StateParser
        {
            WaitName,
            GetName,
            WaitEq,
            WaitContent,
            GetString,
            WaitObj
        }

        private static List<Cell> Parser(string path)
        {
            char[] ignorChar = { ' ', '\n', '\r', '\t' };
            int i = 0;
            char[] buffer = new char[1];
            StringBuilder stringBilder = new StringBuilder();
            FileInfo fi = new FileInfo(path);
            List<Cell> result = new List<Cell>();
            StateParser stat = StateParser.WaitName;
            Cell currentObject = null;
            long idCount = 0;
            if (fi.Exists)
            {
                using (FileStream fstream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(fstream, Encoding.UTF8))
                    {
                        while (reader.Read(buffer, 0, buffer.Length) != 0)
                        {
                            i++;
                            char c = buffer[0];
                            switch (stat)
                            {
                                case StateParser.WaitName:
                                    if (ignorChar.Contains(c))
                                        continue;
                                    if (c == '}')
                                    {
                                        if (currentObject == null)
                                            throw new ArgumentException("Неожиданный символ на позиции " + i.ToString());
                                        if (currentObject.isList)
                                            throw new ArgumentException("Неожиданный символ на позиции " + i.ToString());
                                        currentObject = currentObject.Parent;
                                    }
                                    else
                                    if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_')
                                    {
                                        stringBilder.Append(c);
                                        stat = StateParser.GetName;
                                    }
                                    else
                                        throw new ArgumentException("Неожиданный символ на позиции " + i.ToString());
                                    break;
                                case StateParser.GetName:
                                    if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_' || c >= '0' && c <= '9')
                                        stringBilder.Append(c);
                                    else
                                    if (ignorChar.Contains(c) || c == '=')
                                    {
                                        Cell newCell = new Cell(currentObject,idCount);
                                        idCount++;
                                        newCell.Name = stringBilder.ToString();
                                        stringBilder.Clear();
                                        if (currentObject == null)
                                            result.Add(newCell);
                                        else
                                            currentObject.AddChild(newCell);
                                        currentObject = newCell;
                                        stat = c == '=' ? StateParser.WaitContent : StateParser.WaitEq;
                                    }
                                    else
                                        throw new ArgumentException("Неожиданный символ на позиции " + i.ToString());
                                    break;
                                case StateParser.WaitEq:
                                    if (ignorChar.Contains(c))
                                        continue;
                                    if (c == '=')
                                        stat = StateParser.WaitContent;
                                    else
                                        throw new ArgumentException("Неожиданный символ на позиции " + i.ToString());
                                    break;
                                case StateParser.WaitContent:
                                    if (ignorChar.Contains(c))
                                        continue;
                                    if (c == '"')
                                        stat = StateParser.GetString;
                                    else
                                    if (c == '{')
                                        stat = StateParser.WaitName;
                                    else
                                        throw new ArgumentException("Неожиданный символ на позиции " + i.ToString());
                                    break;
                                case StateParser.GetString:
                                    if (c == '\n' || c == '\r')
                                        throw new ArgumentException("Неожиданный перенос строки на позиции " + i.ToString());
                                    if (c == '"')
                                    {
                                        currentObject.Content = stringBilder.ToString();
                                        stringBilder.Clear();
                                        currentObject = currentObject.Parent;
                                        stat = StateParser.WaitName;
                                    }
                                    else
                                        stringBilder.Append(c);
                                    break;
                            }
                        }
                        if (currentObject != null || stringBilder.Length != 0 || stat != StateParser.WaitName)
                            throw new ArgumentException("Неожиданный конец файла");
                    }
                }
            }
            return result;
        }
    }
}
