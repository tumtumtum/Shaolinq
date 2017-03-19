// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Persistence.Computed
{
	public enum ComputedExpressionToken
	{
		IntegerLiteral,
		StringLiteral,
		Keyword,
		Semicolon,
		Period,
		Assign,
		Add,
		Subtract,
		Multiply,
		Divide,
		Modulo,
		Comma,
		Identifier,
		LeftParen,
		RightParen,
		CompareStart,
		GreaterThan,
		LessThan,
		GreaterThanOrEqual,
		LessThanOrEqual,
		Equals,
		NotEquals,
		CompareEnd,
		LogicalNot,
		QuestionMark,
		DoubleQuestionMark,
		Eof
	}
}