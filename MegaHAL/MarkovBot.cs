using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MarkovBot
{
    public class Markov
    {
        Brain Brain = new Brain();

        public Markov()
        {
            Brain.Banned = new List<string>();
            Brain.KnownWords = new List<string>();
            Brain.Auxiliaries = new List<string>();
            Brain.Forwards = new TreeNode("¬START¬");
            Brain.Backwards = new TreeNode("¬START¬");
            Brain.Forwards.Usage = 1;
            Brain.Backwards.Usage = 1;
        }

        public void Learn(String Input)
        {
            Learn(MakeWords(Input.ToUpper()));
        }

        public void Learn(List<String> Words)
        {
            if (Words.Count <= Brain.Order)
                return;

            Brain.KnownWords.AddRange(Words.Where((x) => !Brain.KnownWords.Contains(x)));

            Context Context = new Context(Brain.Order, Brain.Forwards);
            Context.Impress(Words.ToArray());
            Context.Impress("¬END¬");

            Words.Reverse();
            Context = new Context(Brain.Order, Brain.Backwards);
            Context.Impress(Words.ToArray());
            Context.Impress("¬END¬");
            Words.Reverse();
        }

        public void Train(String Filename)
        {
            String[] Lines = File.ReadAllLines(Filename).Where((x) => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")).ToArray();
            foreach (String Line in Lines)
                Learn(MakeWords(Line.ToUpper()));
        }

        public void LoadBanList(string Filename)
        {
            Brain.Banned.AddRange(File.ReadAllLines(Filename).Where((x) => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")));
        }

        public void LoadAuxiliaries(string Filename)
        {
            Brain.Auxiliaries.AddRange(File.ReadAllLines(Filename).Where((x) => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")));
        }

        public void LoadSwapList(string Filename)
        {
            foreach (String Line in File.ReadAllLines(Filename).Where((x) => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")))
            {
                String[] Halves = Line.Split(',');
                Brain.Swaps.Add(new Tuple<String, String>(Halves[0], Halves[1]));
            }
        }

        public void LoadBrain(string Filename)
        {
            FileStream FS = File.Open(Filename, FileMode.Open);
            BinaryFormatter BF = new BinaryFormatter();
            Brain = (Brain)BF.Deserialize(FS);
            FS.Close();
        }

        public void SaveBrain(string Filename)
        {
            FileStream FS = File.Open(Filename, FileMode.Create);
            BinaryFormatter BF = new BinaryFormatter();
            BF.Serialize(FS, Brain);
            FS.Close();
        }

        public Boolean Audit()
        {
            Stack<TreeNode> VisitStack = new Stack<TreeNode>();

            VisitStack.Push(Brain.Forwards);
            VisitStack.Push(Brain.Backwards);

            while (VisitStack.Count > 0)
            {
                TreeNode Node = VisitStack.Pop();
                if (Node.Visited)
                    return false;
                Node.Visited = true;
                foreach (TreeNode SubNode in Node.SubNodes)
                    VisitStack.Push(SubNode);
            }
            return true;
        }

        private List<String> MakeWords(String Line)
        {
            if (String.IsNullOrWhiteSpace(Line))
                return new List<string>();

            Int32 Offset = 0;
            List<String> Words = new List<string>();

            while (true)
            {

                if (Boundary(Line, Offset))
                {
                    Words.Add(Line.Substring(0, Offset));

                    if (Offset == Line.Length)
                        break;

                    Line = Line.Substring(Offset);
                    Offset = 0;
                }
                else
                    Offset++;
            }

            String LastWord = Words[Words.Count - 1];

            if (Char.IsLetterOrDigit(LastWord[0]))
            {
                Words.Add(".");
            }
            else if (!("!.?".Contains(LastWord[LastWord.Length - 1])))
            {
                Words[Words.Count - 1] = ".";
            }

            return Words;
        }

        private Boolean Boundary(String Buffer, Int32 Position)
        {
            if (Position == 0)
                return false;

            if (Position == Buffer.Length)
                return true;

            if (Buffer[Position - 1] == '\'' && Char.IsLetter(Buffer[Position - 1]) && Char.IsLetter(Buffer[Position + 1]))
                return false;

            if (Position > 1 && Buffer[Position - 1] == '\'' && Char.IsLetter(Buffer[Position - 2]) && Char.IsLetter(Buffer[Position]))
                return false;

            if (Char.IsLetter(Buffer[Position]) && !Char.IsLetter(Buffer[Position - 1]))
                return true;

            if (!Char.IsLetter(Buffer[Position]) && Char.IsLetter(Buffer[Position - 1]))
                return true;

            if (Char.IsDigit(Buffer[Position - 1]) != Char.IsDigit(Buffer[Position]))
                return true;

            return false;
        }

        public String GenerateReply(String Input)
        {
            Input = Input.ToUpper();
            return GenerateReply(MakeWords(Input));
        }

        private String GenerateReply(List<String> Input)
        {
            String Output = "I'm speechless.";
            List<String> Keywords = MakeKeywords(Input);

            List<String> Replies = Reply(new List<String>());
            if (Dissimilar(Input, Replies))
                Output = MakeOutput(Replies);

            float Surprise = 0.0f;
            float MaxSurprise = -1.0f;

            Int32 Count = 0;

            for (Int32 X = 0; X < 64; X++)
            {
                Replies = Reply(Keywords);
                Surprise = EvaluateReply(Keywords, Replies);
                Count++;
                if ((Surprise > MaxSurprise) && Dissimilar(Input, Replies))
                {
                    MaxSurprise = Surprise;
                    Output = MakeOutput(Replies);
                }
            }
            Learn(Input);
            return Output;
        }

        private Boolean Dissimilar(List<String> Words1, List<String> Words2)
        {
            if (Words1.Count != Words2.Count)
                return true;

            for (Int32 X = 0; X < Math.Min(Words1.Count, Words2.Count); X++)
                if (Words1[X] != Words2[X])
                    return true;

            return false;
        }

        private float EvaluateReply(List<String> Keys, List<String> Words)
        {
            if (Words.Count == 0)
                return 0.0f;

            Dictionary<String, Int32> WordUsage = new Dictionary<String, Int32>();
            Int32 WordCount = 0;

            Int32 Num = 0;
            float Entropy = 0.0f;
            float Probability = 0.0f;

            Context Context = new Context(Brain.Order, Brain.Forwards);

            foreach (String Word in Words)
            {
                if (WordUsage.ContainsKey(Word) && !String.IsNullOrWhiteSpace(Word))
                {
                    WordUsage[Word]++;
                    WordCount++;
                }
                else if (!String.IsNullOrWhiteSpace(Word))
                {

                    WordCount++;
                    WordUsage.Add(Word, 1);
                }

                if (Keys.Contains(Word))
                {
                    Probability = 0.0f;
                    Int32 Count = 0;
                    Num++;

                    for (Int32 X = 0; X < Brain.Order; X++)
                    {
                        if (Context.LinkValid(X))
                        {
                            TreeNode Node = Context.FindSubNode(X, Word);
                            Probability += (float)Node.Usage / (float)Context.Node(X).Usage;
                            Count++;
                        }
                    }

                    if (Count > 0)
                        Entropy -= (float)Math.Log(Probability / (float)Count);

                }
                Context.UpdateContext(Word);
            }

            foreach (String Word in WordUsage.Keys) {
                if (WordUsage[Word] >= (WordCount / (Math.Log(Words.Count) / Math.Log(2))))
                    return -1.0f; // Hack - if any single word shows up too much (spaces don't count), declare the sentence as untenable
            }

            Context = new Context(Brain.Order, Brain.Backwards);

            Words.Reverse();

            foreach (String Word in Words)
            {
                if (Keys.Contains(Word))
                {
                    Probability = 0.0f;
                    Int32 Count = 0;
                    Num++;

                    for (Int32 X = 0; X < Brain.Order; X++)
                    {
                        if (Context.LinkValid(X))
                        {
                            TreeNode Node = Context.FindSubNode(X, Word);
                            Probability += (float)Node.Usage / (float)Context.Node(X).Usage;
                            Count++;
                        }
                    }

                    if (Count > 0)
                        Entropy -= (float)Math.Log(Probability / (float)Count);

                }
                Context.UpdateContext(Word);
            }

            Words.Reverse();

            if (Num >= 8)
                Entropy /= (float)Math.Sqrt(Num - 1);
            if (Num >= 16)
                Entropy /= (float)Num;

            return Entropy;

        }

        private String MakeOutput(List<String> Words)
        {
            if (Words.Count == 0)
                return "I'm Speechless.";

            return String.Join("", Words.ToArray());
        }

        static public Boolean UsedKey = false;

        private List<String> Reply(List<String> Keys)
        {
            UsedKey = false;
            Boolean Start = true;
            List<String> ReplyWords = new List<string>();
            String Symbol = "";

            Context Context = new Context(Brain.Order, Brain.Forwards);

            Int32 Num = 0;
            while (Num < 1024)
            {
                if (Start)
                    Symbol = Seed(Context, Keys);
                else
                    Symbol = Babble(Context, Keys, ReplyWords);
                if (Symbol == "¬START¬" || Symbol == "¬END¬")
                    break;

                Start = false;
                Num++;
                ReplyWords.Add(Symbol);
                Context.UpdateContext(Symbol);
            }


            Context = new Context(Brain.Order, Brain.Backwards);

            for (Int32 X = Math.Min(Brain.Order, ReplyWords.Count - 1); X >= 0; X--)
                Context.UpdateContext(ReplyWords[X]);

            Num = 0;
            while (Num < 1024)
            {
                Symbol = Babble(Context, Keys, ReplyWords);
                if (Symbol == "¬START¬" || Symbol == "¬END¬")
                    break;

                Num++;
                ReplyWords.Insert(0, Symbol);
                Context.UpdateContext(Symbol);

            }

            return ReplyWords;
        }

        private String Babble(Context Context, List<String> Keys, List<String> Replies)
        {
            TreeNode Node = Context.LongestContext();

            Node.Visited = true;

            if (Node.SubNodes.Count == 0)
                return "¬END¬";

            String Symbol = "¬END¬";
            Random RNG = new Random();


            Int32 I = RNG.Next(0, Node.SubNodes.Count);
            Int32 Count = RNG.Next(0, Node.Usage);

            while (Count >= 0)
            {
                Symbol = Node.SubNodes[I].Item;
                if (Keys.Contains(Symbol) && (UsedKey || !Brain.Auxiliaries.Contains(Symbol)) && !Replies.Contains(Symbol))
                {
                    UsedKey = true;
                    break;
                }
                Count -= Node.SubNodes[I].Usage;
                I = (I >= (Node.SubNodes.Count - 1)) ? 0 : I + 1;
            }


            return Symbol;
        }

        private String Seed(Context Context, List<String> Keys)
        {
            String Symbol = "";

            if (Context.Branches() == 0)
                Symbol = "¬START¬";
            else
                Symbol = Context.Pick().Item;

            Random RNG = new Random();
            if (Keys.Count > 0)
            {
                Int32 I = RNG.Next(0, Keys.Count);
                Int32 Stop = I;
                while (true)
                {

                    if (Brain.KnownWords.Contains(Keys[I]) && !Brain.Auxiliaries.Contains(Keys[I]))
                    {
                        return Keys[I];
                    }

                    I++;
                    if (I == Keys.Count)
                        I = 0;
                    if (I == Stop)
                        return Symbol;
                }
            }
            return Symbol;
        }

        public List<String> MakeKeywords(List<String> Words)
        {
            List<String> Keywords = new List<String>();

            foreach (String Word in Words)
            {
                Boolean Swapped = false;
                foreach (Tuple<String, String> Key in Brain.Swaps)
                {
                    if (Key.Item1 == Word)
                    {
                        if (!Keywords.Contains(Key.Item2))
                            Keywords.Add(Key.Item2);
                        Swapped = true;
                    }
                }
                if (!Swapped && !IsInvalidKeyword(Word) && !Keywords.Contains(Word))
                    Keywords.Add(Word);
            }

            if (Keywords.Count > 0)
                foreach (String Word in Words)
                {
                    Boolean Added = false;
                    foreach (Tuple<String, String> Key in Brain.Swaps)
                    {
                        if (Key.Item1 == Word)
                        {
                            if (!Keywords.Contains(Key.Item2))
                                Keywords.Add(Key.Item2);
                            Added = true;
                        }
                    }
                    if (!Added && !IsInvalidAuxiliary(Word) && !Keywords.Contains(Word))
                        Keywords.Add(Word);
                }

            return Keywords;
        }

        private Boolean IsInvalidKeyword(String Key)
        {
            if (Brain.Banned.Contains(Key))
                return true;
            if (Brain.Auxiliaries.Contains(Key))
                return true;
            if (!Char.IsLetterOrDigit(Key[0]))
                return true;
            if (!Brain.KnownWords.Contains(Key))
                return true;

            return false;
        }

        private Boolean IsInvalidAuxiliary(String Aux)
        {
            if (Brain.Banned.Contains(Aux))
                return true;
            if (!Char.IsLetterOrDigit(Aux[0]))
                return true;
            if (!Brain.KnownWords.Contains(Aux))
                return true;

            return false;
        }
    }
}
