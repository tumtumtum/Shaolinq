// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Shaolinq.Persistence.Computed
{
	public class ComputedExpressionTokenizer
	{
		private int currentChar;
		private readonly TextReader reader;
		private readonly StringBuilder stringBuilder;
		public string CurrentString { get; private set; }
		public string CurrentIdentifier { get; private set; }
		public long CurrentInteger { get; private set; }
		public string CurrentKey => this.CurrentIdentifier;
		public ComputedExpressionToken CurrentToken { get; private set; }
		public ComputedExpressionKeyword CurrentKeyword { get; private set; }

		public bool CurrentTokenMatches(params ComputedExpressionToken[] tokens)
		{
			return tokens.Contains(this.CurrentToken);
		}

		public ComputedExpressionTokenizer(TextReader reader)
		{
			this.reader = reader;
			this.stringBuilder = new StringBuilder();

			this.ConsumeChar();
		}

		private void ConsumeChar()
		{
			this.currentChar = this.reader.Read();
		}

		public ComputedExpressionToken ReadNextToken()
		{
			while (Char.IsWhiteSpace((char)this.currentChar) || this.currentChar == '\0')
			{
				this.ConsumeChar();
			}

			if (this.currentChar == -1)
			{
				this.CurrentToken = ComputedExpressionToken.Eof;

				return this.CurrentToken;
			}

			if (this.currentChar == '=')
			{
				this.ConsumeChar();

				if (this.currentChar == '=')
				{
					this.ConsumeChar();

					this.CurrentToken = ComputedExpressionToken.Equals;
				}
				else
				{
					this.CurrentToken = ComputedExpressionToken.Assign;
				}

				return this.CurrentToken;
			}
			else if (this.currentChar == '?')
			{
				this.ConsumeChar();

				if (this.currentChar == '?')
				{
					this.ConsumeChar();
					this.CurrentToken = ComputedExpressionToken.DoubleQuestionMark;
				}
				else
				{
					this.CurrentToken = ComputedExpressionToken.QuestionMark;
				}

				return this.CurrentToken;
			}
			else if (this.currentChar == '!')
			{
				this.ConsumeChar();

				if (this.currentChar == '=')
				{
					this.ConsumeChar();

					this.CurrentToken = ComputedExpressionToken.NotEquals;
				}
				else
				{
					this.CurrentToken = ComputedExpressionToken.LogicalNot;
				}

				return this.CurrentToken;
			}
			else if (this.currentChar == '&')
			{
				this.ConsumeChar();

				if (this.currentChar == '&')
				{
					this.ConsumeChar();

					this.CurrentToken = ComputedExpressionToken.LogicalAnd;
				}
				else
				{
					this.CurrentToken = ComputedExpressionToken.BitwiseAnd;
				}

				return this.CurrentToken;
			}
			else if (this.currentChar == '|')
			{
				this.ConsumeChar();

				if (this.currentChar == '|')
				{
					this.ConsumeChar();

					this.CurrentToken = ComputedExpressionToken.LogicalOr;
				}
				else
				{
					this.CurrentToken = ComputedExpressionToken.BitwiseOr;
				}

				return this.CurrentToken;
			}
			else if (this.currentChar == '<')
			{
				this.ConsumeChar();

				if (this.currentChar == '=')
				{
					this.ConsumeChar();

					this.CurrentToken = ComputedExpressionToken.LessThanOrEqual;
				}
				else
				{
					this.CurrentToken = ComputedExpressionToken.LessThan;
				}

				return this.CurrentToken;
			}
			else if (this.currentChar == '>')
			{
				this.ConsumeChar();

				if (this.currentChar == '=')
				{
					this.ConsumeChar();

					this.CurrentToken = ComputedExpressionToken.GreaterThanOrEqual;
				}
				else
				{
					this.CurrentToken = ComputedExpressionToken.GreaterThan;
				}

				return this.CurrentToken;
			}
			else if (this.currentChar == '.')
			{
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Period;

				return this.CurrentToken;
			}
			else if (this.currentChar == ';')
			{
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Semicolon;

				return this.CurrentToken;
			}
			else if (this.currentChar == ',')
			{
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Comma;

				return this.CurrentToken;
			}
			else if (this.currentChar == '(')
			{
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.LeftParen;

				return this.CurrentToken;
			}
			else if (this.currentChar == ')')
			{
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.RightParen;

				return this.CurrentToken;
			}
			else if (this.currentChar == '+')
			{
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Add;

				return this.CurrentToken;
			}
			else if (this.currentChar == '-')
			{
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Subtract;

				return this.CurrentToken;
			}
			else if (this.currentChar == '*')
			{
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Multiply;

				return this.CurrentToken;
			}
			else if (this.currentChar == '/')
			{
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Divide;

				return this.CurrentToken;
			}
			else if (this.currentChar == '%')
			{
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Modulo;

				return this.CurrentToken;
			}
			else if (Char.IsDigit((char)this.currentChar))
			{
				this.stringBuilder.Length = 0;

				while (this.currentChar != -1 && (Char.IsLetterOrDigit((char)this.currentChar)))
				{
					this.stringBuilder.Append((char)this.currentChar);

					this.ConsumeChar();
				}
				
				if (this.currentChar == 'l' || this.currentChar == 'L')
				{
					this.CurrentToken = ComputedExpressionToken.LongLiteral;

					this.ConsumeChar();
				}
				else
				{
					this.CurrentToken = ComputedExpressionToken.IntegerLiteral;
				}

				var s = this.stringBuilder.ToString();

				this.CurrentInteger = Convert.ToInt64(s);

				if (this.CurrentToken == ComputedExpressionToken.IntegerLiteral && (this.CurrentInteger <= int.MinValue || this.CurrentInteger > int.MaxValue))
				{
					throw new InvalidOperationException($"The integer {s} exceeds the maximum size of an integer");
				}

				return this.CurrentToken;
			}
			else if (Char.IsLetter((char)this.currentChar))
			{
				this.stringBuilder.Length = 0;

				while (this.currentChar != -1 && (Char.IsLetterOrDigit((char)this.currentChar) ))
				{
					this.stringBuilder.Append((char)this.currentChar);

					this.ConsumeChar();
				}

				ComputedExpressionKeyword keyword;;
				var s = this.stringBuilder.ToString();

				if (Enum.TryParse(s, true, out keyword))
				{
					this.CurrentToken = ComputedExpressionToken.Keyword;
					this.CurrentKeyword = keyword;
				}
				else
				{
					this.CurrentToken = ComputedExpressionToken.Identifier;
					this.CurrentIdentifier = s;
				}

				return this.CurrentToken;
			}
			else if (this.currentChar == '"')
			{
				var previousChar = -1;
				var startChar = this.currentChar;

				this.stringBuilder.Length = 0;

				this.ConsumeChar();

				while (this.currentChar != -1 && (this.currentChar != startChar || (this.currentChar == startChar && previousChar == '\\')))
				{
					if (this.currentChar == startChar)
					{
						if (previousChar == '\\')
						{
							this.stringBuilder.Remove(this.stringBuilder.Length - 1, 1);
							this.stringBuilder.Append(startChar);
							this.ConsumeChar();

							continue;
						}
						else
						{
							break;
						}
					}

					this.stringBuilder.Append((char)this.currentChar);
					previousChar = this.currentChar;

					this.ConsumeChar();
				}

				if (this.currentChar == startChar)
				{
					this.ConsumeChar();
				}

				var s = this.stringBuilder.ToString();

				this.CurrentToken = ComputedExpressionToken.StringLiteral;
				this.CurrentString = s;

				return this.CurrentToken;
			}
			
			throw new InvalidOperationException($"Unexpected character: {(char) this.currentChar}");
		}
	}
}
