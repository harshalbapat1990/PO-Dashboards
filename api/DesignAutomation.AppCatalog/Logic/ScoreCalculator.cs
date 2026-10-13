using System;
using DesignAutomation.AppCatalog.Model;

namespace DesignAutomation.AppCatalog.Logic;

public static class ScoreCalculator
{
	public static int Score(this ReadmeData app)
	{
		int score = 0;
		if (!String.IsNullOrWhiteSpace(app.Description))
			score += 1;
		if (app.Team.Count > 0)
			score += 1;
		if (!String.IsNullOrWhiteSpace(app.RepoName))
			score += 1;
		if (!String.IsNullOrWhiteSpace(app.ProductionBranch))
			score += 1;
		if (app.Team.Count > 0)
			score += 1;
		if (app.Documents.Count > 0)
			score += 1;
		if (app.TechStack.Count > 0)
			score += 1;
		if (!String.IsNullOrWhiteSpace(app.ProjectManagement.URL))
			score += 1;

		return Convert.ToInt32((score / 8.0) * 100.0);
	}

	/// <summary>Returns max score of completeness</summary>
	public static int Score(this AppHeader app)
	{
		// max is 13 for now
		int score = 0;
		if (!String.IsNullOrWhiteSpace(app.Description))
			score += 1;
		if (app.Team.Count > 0)
			score += 1;
		if (app.Repos.Count > 0)
			score += 1;
		if (!String.IsNullOrWhiteSpace(app.ProductionBranch))
			score += 1;
		if (app.DocLinks.Count > 0)
			score += 1;
		if (app.PMLinks.Count > 0)
			score += 1;
		if (!String.IsNullOrWhiteSpace(app.Hosting))
			score += 1;
		if (!String.IsNullOrWhiteSpace(app.AppType))
			score += 1;
		if (!String.IsNullOrWhiteSpace(app.TechStack))
			score += 1;
		if (!String.IsNullOrWhiteSpace(app.Environments))
			score += 1;
		if (!String.IsNullOrWhiteSpace(app.IdentityManagement))
			score += 1;
		if (!String.IsNullOrWhiteSpace(app.Scale))
			score += 1;
		if (!String.IsNullOrWhiteSpace(app.Growth))
			score += 1;

		return Convert.ToInt32((score / 13.0) * 100.0);
	}
}
