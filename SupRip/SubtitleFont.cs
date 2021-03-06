using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace SupRip
{
	class SubtitleFont
	{
		public enum FontType
		{
			ProgramFont = 1, UserFont = 2
		}

		private LinkedList<SubtitleLetter> letters;
		private FontType type;
		private string fontName, fileName;
		private bool changed;

		public string Name
		{
			get { return fontName; }
		}

		public bool Changed
		{
			get { return changed; }
		}

		public SubtitleFont(FontType t, string fn)
		{
			type = t;
			fontName = fn;

			letters = new LinkedList<SubtitleLetter>();

			if (type == FontType.ProgramFont)
				fileName = Application.StartupPath + "\\" + fontName + ".font.txt";
			else
				fileName = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SupRip\\" + fontName + ".font.txt";

			if (File.Exists(fileName))
			{
				StreamReader sr = new StreamReader(fileName);

				string text;

				// Skip past the version line
				text = sr.ReadLine();

				while ((text = sr.ReadLine()) != null)
				{
					string[] arraySize = sr.ReadLine().Trim().Split(' ');

					if (arraySize.Length != 2)
						throw new FontfileFormatException("arraySize is screwed up: " + arraySize);

					int w = Int32.Parse(arraySize[0]);
					int h = Int32.Parse(arraySize[1]);
					byte[,] letterArray = new byte[h, w];
					for (int j = 0; j < h; j++)
					{
						string[] arrayLine = sr.ReadLine().Trim().Split(' ');
						if (arrayLine.Length != w)
							throw new FontfileFormatException("arrayLine is " + arrayLine.Length + " instead of " + w);

						for (int i = 0; i < w; i++)
							letterArray[j, i] = Byte.Parse(arrayLine[i]);
					}

					letters.AddLast(new SubtitleLetter(letterArray, text));

					// Skip the empty line between letters
					sr.ReadLine();
				}


				sr.Close();
			}
		}

		/// <summary>
		/// Moves all the letters from this font to another font
		/// </summary>
		/// <param name="target">The target font that the letters should be moved to</param>
		public void MoveLetters(SubtitleFont target)
		{
			foreach (SubtitleLetter letter in letters)
				target.AddLetter(letter);

			letters.Clear();
			changed = true;
		}

		public void AddLetter(SubtitleLetter l)
		{
			changed = true;
			letters.AddLast(l);
		}

		public string ListDuplicates()
		{
			string r = "";

			string[] chars = new string[letters.Count];
			int k = 0;
			foreach (SubtitleLetter l in letters)
				chars[k++] = l.Text;

			for (int j = 0; j < letters.Count; j++)
			{
				for (int i = j + 1; i < letters.Count; i++)
				{
					if (chars[i] == chars[j])
						r += " " + chars[i];
				}
			}

			return r;
		}

		public SortedList<int, SubtitleLetter> FindMatch(SubtitleLetter l, int tolerance)
		{
			//if (l.Coords.X != 598 && l.Coords.X != 326) return new SortedList<int, SubtitleLetter>();

			SortedList<int, SubtitleLetter> results = new SortedList<int, SubtitleLetter>();

			foreach (SubtitleLetter letter in letters)
			{
				if (!letter.BordersMatch(l))
					continue;

				int similarity = letter.Matches(l);
				if (similarity == 0)
				{
					//if (debug) Debugger.Print("perfect match " + letter.Text);
					results.Add(similarity, letter);
					return results;
				}

				if (AppOptions.IsEasilyConfusedLetter(letter.Text))
					similarity *= 10;

				if (similarity < tolerance && !results.ContainsKey(similarity))
					results.Add(similarity, letter);
			}

			//if (debug && results.Count > 0) Debugger.Print("decided on " + results.Values[0].Text + "\n");
			return results;
			// If we haven't found an exact match, return the best one.
		}

		public void Save()
		{
			if (fileName == null)
				throw new Exception("filename is null on saving a font");

			string folder = fileName.Substring(0, fileName.LastIndexOf('\\'));
			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);

			StreamWriter tw = new StreamWriter(new FileStream(fileName, FileMode.Create));

			tw.WriteLine("version 2");
			foreach (SubtitleLetter letter in letters)
			{
				tw.WriteLine(letter.Text);
				tw.WriteLine("{0} {1}", letter.ImageWidth, letter.ImageHeight);
				tw.WriteLine(letter.DumpLetter());
			}

			tw.Close();
		}

		public void DeleteLetter(SubtitleLetter l2)
		{
			changed = true;
			letters.Remove(l2);
		} 

		public static void DeleteUserFont(string name)
		{
			string filename = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SupRip\\" + name + ".font.txt";

			File.Delete(filename);
		}
	}
}
