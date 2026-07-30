namespace RevenueMetrics.Application.Exceptions;

public class ExpiredCursorException : Exception
{
	public ExpiredCursorException(string message) : base(message)
	{
	}
}
