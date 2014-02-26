using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Newtonsoft.Json.Linq;
using VersionOne.CommitService.Interfaces;
using VersionOne.CommitService.Types;

namespace VersionOne.CommitService.Plugin.Translator.GitHub
{
	public class GitHubTranslator : ITranslateInboundMessageToCommits
	{
		public Translation.Result Execute(InboundMessage message)
		{
			try
			{
				IEnumerable<CommitMessage> commitMessages = GetCommitMessages(message.Body);
				return Translation.Success(commitMessages);
			}
			catch
			{
				return Translation.Failure("It was not possible to translate the message.");
			}
		}

		public bool CanProcess(InboundMessage message)
		{
			return ContainsGitHubEventHeaderAndIsPush(message);
		}

		private IEnumerable<CommitMessage> GetCommitMessages(string body)
		{
			dynamic root = JObject.Parse(body);
			var commitMessages = new List<CommitMessage>();

			foreach (var commit in root.commits)
			{
				var commitMessage = new CommitMessage(
					new Author()
					{
						Email = commit.author.email.ToString(),
						Name = commit.author.name.ToString(),
						Url = GetUserUrl(commit.author.username.ToString())
					},
					DateTimeOffset.Parse(commit.timestamp.ToString()),
					commit.message.ToString(),
					new Repo()
					{
						Name = root.repository.name.ToString(),
						Url = root.repository.url.ToString()
					},
					"GitHub",
					new CommitId()
					{
						Name = commit.id.ToString(),
						Url = commit.url.ToString()
					},
					null
					);

				commitMessages.Add(commitMessage);
			}

			return commitMessages;
		}

		private string GetUserUrl(string username)
		{
			return string.Format("https://github.com/{0}", username);
		}

		private bool ContainsGitHubEventHeaderAndIsPush(InboundMessage message)
		{
			return message.Headers.ContainsKey("X-Github-Event")
				&& message.Headers["X-Github-Event"].Contains("push");
		}
	}
}