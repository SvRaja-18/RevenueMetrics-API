using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevenueMetrics.Domain.Policies;

public static class RevenuePolicy
{
	private static readonly HashSet<string> CollectedStatuses =
		new(StringComparer.OrdinalIgnoreCase)
		{
			"paid",
			"succeeded",
			"completed"
		};

	public static bool IsCollected(string status)
	{
		return CollectedStatuses.Contains(status);
	}
}
