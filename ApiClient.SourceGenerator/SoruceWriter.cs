using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Text;

namespace ApiClient.SourceGenerator
{
	internal sealed class SoruceWriter
	{
		private const char IndentationChar = ' ';
		private const int CharsPerIndentation = 4;

		private readonly StringBuilder _sb = new();
		private int _indentation = 0;

		public int Indentation
		{
			get => _indentation;
			set
			{
				if (value < 0)
				{
					Throw();
					static void Throw() => throw new ArgumentOutOfRangeException(nameof(value));
				}

				_indentation = value;
			}
		}

		public void WriteLine(char value)
		{
			AddIndentation();
			_sb.Append(value);
			_sb.AppendLine();
		}

		public void WriteLine(string text)
		{
			if (_indentation == 0)
			{
				_sb.AppendLine(text);
				return;
			}

			bool isFinalLine;
			ReadOnlySpan<char> remainingText = text.AsSpan();
			do
			{
				string nextLine = GetNextLine(ref remainingText, out isFinalLine);

				AddIndentation();
				_sb.AppendLine(nextLine);
			}
			while (!isFinalLine);
		}

		public SourceText ToSourceText()
		{
			Debug.Assert(_indentation == 0 && _sb.Length > 0);
			return SourceText.From(_sb.ToString(), Encoding.UTF8);
		}

		public void AppendLine() => _sb.AppendLine();

		public void BeginBlock()
		{
			WriteLine("{");
			Indentation++;
		}

		public void EndBlock()
		{
			Indentation--;
			WriteLine("}");
		}

		private void AddIndentation() => _sb.Append(IndentationChar, CharsPerIndentation * _indentation);

		private static string GetNextLine(ref ReadOnlySpan<char> remainingText, out bool isFinalLine)
		{
			if (remainingText.IsEmpty)
			{
				isFinalLine = true;
				return string.Empty;
			}

			ReadOnlySpan<char> next;
			ReadOnlySpan<char> rest;

			int lineLength = remainingText.IndexOf('\n');
			if (lineLength == -1)
			{
				lineLength = remainingText.Length;
				isFinalLine = true;
				rest = default;
			}
			else
			{
				rest = remainingText.Slice(lineLength + 1);
				isFinalLine = false;
			}

			if ((uint)lineLength > 0 && remainingText[lineLength - 1] == '\r')
			{
				lineLength--;
			}

			next = remainingText.Slice(0, lineLength);
			remainingText = rest;
			return next.ToString();
		}

	}
}
