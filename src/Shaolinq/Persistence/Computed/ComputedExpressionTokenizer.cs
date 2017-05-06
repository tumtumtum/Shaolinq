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
		public float CurrentFloat { get; private set; }
		public double CurrentDouble { get; private set; }
		public decimal CurrentDecimal { get; private set; }
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

			switch (this.currentChar)
			{
			case '=':
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
			case '?':
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
			case '!':
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
			case '&':
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
			case '|':
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
			case '<':
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
			case '>':
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
			case '.':
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Period;

				return this.CurrentToken;
			case ';':
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Semicolon;

				return this.CurrentToken;
			case ',':
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Comma;

				return this.CurrentToken;
			case '(':
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.LeftParen;

				return this.CurrentToken;
			case ')':
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.RightParen;

				return this.CurrentToken;
			case '+':
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Add;

				return this.CurrentToken;
			case '-':
				this.ConsumeChar();
			
				this.CurrentToken = ComputedExpressionToken.Subtract;

				return this.CurrentToken;
			case '*':
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Multiply;

				return this.CurrentToken;
			case '/':
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Divide;

				return this.CurrentToken;
			case '%':
				this.ConsumeChar();
				this.CurrentToken = ComputedExpressionToken.Modulo;

				return this.CurrentToken;
			default:
				if (Char.IsDigit((char)this.currentChar))
				{
					this.stringBuilder.Length = 0;
					var floatingPoint = false;

					while (this.currentChar != -1 && (Char.IsLetterOrDigit((char)this.currentChar) || (this.currentChar == '.' && !floatingPoint)))
					{
						if (this.currentChar == '.')
						{
							floatingPoint = true;
						}

						this.stringBuilder.Append((char)this.currentChar);

						this.ConsumeChar();
					}

					var s = this.stringBuilder.ToString();

					if (floatingPoint && this.currentChar == 'f')
					{
						this.CurrentFloat = Convert.ToSingle(s);
						this.CurrentToken = ComputedExpressionToken.FloatLiteral;
						this.ConsumeChar();
					}
					else if (floatingPoint && this.currentChar == 'd')
					{
						this.CurrentDouble = Convert.ToDouble(s);
						this.CurrentToken = ComputedExpressionToken.DoubleLiteral;
						this.ConsumeChar();
					}
					else if (floatingPoint && this.currentChar == 'm')
					{
						this.CurrentDecimal = Convert.ToDecimal(s);
						this.CurrentToken = ComputedExpressionToken.DecimalLiteral;
						this.ConsumeChar();
					}
					else if (floatingPoint)
					{
						this.CurrentFloat = Convert.ToSingle(s);
						this.CurrentToken = ComputedExpressionToken.FloatLiteral;
					}
					else if (this.currentChar == 'l' || this.currentChar == 'L')
					{
						this.CurrentToken = ComputedExpressionToken.LongLiteral;
						this.CurrentInteger = Convert.ToInt64(s);
						this.ConsumeChar();
					}
					else
					{
						this.CurrentToken = ComputedExpressionToken.IntegerLiteral;
						this.CurrentInteger = Convert.ToInt32(s);
					}

					return this.CurrentToken;
				}
				else if (char.IsLetter((char)this.currentChar))
				{
					this.stringBuilder.Length = 0;

					while (this.currentChar != -1 && char.IsLetterOrDigit((char)this.currentChar))
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
				break;
			}
			
			throw new InvalidOperationException($"Unexpected character: {(char) this.currentChar}");
		}
	}
}
