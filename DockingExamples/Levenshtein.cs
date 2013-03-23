using System;
using Docking.Components;
using Gtk;
using System.Collections.Generic;

namespace Examples
{
    [System.ComponentModel.ToolboxItem(false)]
    public partial class Levenshtein : Gtk.Bin
    {
        public Levenshtein()
        {
            this.Build();
            this.Name = "Levenshtein"; 
            m_Buffer = textview.Buffer;
            m_Buffer.Text = "In information theory and computer science, the Levenshtein distance is a string metric for measuring the difference between two sequences. Informally, the Levenshtein distance between two words is the minimum number of single-character edits required to change one word into the other. The phrase edit distance is often used to refer specifically to Levenshtein distance. The Levenshtein distance between two strings is defined as the minimum number of edits needed to transform one string into the other, with the allowable edit operations being insertion, deletion, or substitution of a single character. It is named after Vladimir Levenshtein, who considered this distance in 1965. It is closely related to pairwise string alignments."
                + "\n\nFor example, the Levenshtein distance between \"kitten\" and \"sitting\" is 3, since the following three edits change one into the other, and there is no way to do it with fewer than three edits:"
                    + "\n\nkitten → sitten (substitution of \"s\" for \"k\")"
                    + "\nsitten → sittin (substitution of \"i\" for \"e\")"
                    + "\nsittin → sitting (insertion of \"g\" at the end).";

            TextTag tag = new TextTag("bold");
            tag.Weight = Pango.Weight.Bold;
            tag.Background = "yellow";
            m_Buffer.TagTable .Add(tag);

            entryLine.Changed += (sender, e) => 
            {
                SearchFor(entryLine.Text);
            };

            m_Buffer.Changed += (sender, e) => 
            {
                TokenizeText();
                SearchFor(entryLine.Text);
            };

            TokenizeText();
            labelBestMatch.LabelProp = "- ";
        }


        TextBuffer m_Buffer;
        String [] m_Token;

        void TokenizeText()
        {
            m_Token = textview.Buffer.Text.Split(new char[] {' ', '.', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }

        void SearchFor(String txt)
        {
            m_Buffer.RemoveAllTags(m_Buffer.GetIterAtOffset(0), m_Buffer.GetIterAtOffset(int.MaxValue));
            labelBestMatch.LabelProp = "- ";
            if (txt.Length > 0)
            {
                Byte bestKey = 255;
                SortedDictionary<Byte, List<string>> matches = new SortedDictionary<byte, List<string>>();
                Docking.Tools.ITextMetric tm = new Docking.Tools.Levenshtein(50);
                foreach(string s in m_Token)
                {
                    Byte d = tm.distance(txt, s);
                    if (d <= bestKey) // ignore worse matches as previously found
                    {
                        bestKey = d;
                        List<string> values;
                        if (!matches.TryGetValue(d, out values))
                        {
                            values = new List<string>();
                            matches.Add(d, values);
                        }
                        if (!values.Contains(s))
                            values.Add(s);
                    }
                }

                if (matches.Count > 0)
                {
                    List<string> result = matches[bestKey];
                    foreach(string s in result)
                        MarkAll(s);
                    labelBestMatch.LabelProp = bestKey.ToString() + " ";
                }
            }
        }

        void MarkAll(string exp)
        {
            TextIter start, end;
            start = m_Buffer.GetIterAtOffset(0);
            TextIter limit = m_Buffer.GetIterAtOffset(int.MaxValue);
            while(start.ForwardSearch(exp, TextSearchFlags.TextOnly, out start, out end, limit))
            {
                m_Buffer.ApplyTag("bold", start, end);
                start.Offset++;
            }
        }
    }

    #region Starter / Entry Point
    
    public class LevenshteinFactory : ComponentFactory
    {
        public override Type TypeOfInstance { get { return typeof(Levenshtein); } }
        public override String MenuPath { get { return @"View\Examples\Levenshtein"; } }
        public override String Comment { get { return "Levenshtein example"; } }
        public override Mode Options { get { return Mode.CloseOnHide; } }
        public override Gdk.Pixbuf Icon { get { return Gdk.Pixbuf.LoadFromResource ("Examples.HelloWorld-16.png"); } }
    }
    
    #endregion

}

