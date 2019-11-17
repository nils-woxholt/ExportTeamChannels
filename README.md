# ExportTeamChannels
Export all channels, posts and replies from a Microsoft Teams team

This is a project that I made to export a team and its messages from Microsoft Teams.
I needed to product an offline copy of all the messages and replies following a hackathon that I supported.
We had extensively used teams to provide support for remote participants and felt that there was a decent amount of information shared in the team that might be helpful to others going forward.

In this project I used the Microsoft Graph API to export the data.

With the data, I create 2 output folders: 1 for channels and 1 for messages.

Then for each channel and message I create a json file with the raw data and a markdown file.

I used the markdown files as channel and message collections in Jekyll to create a static website from the data.

# Usages
This is a console app

Run the app with 2 command line paramaters:

-t <teamid> This is the team id of the Team that you want to export

-b <bearertoken> The bearer token for the graph api 
  (get this by logging in using the Microsoft Graph Explorer)
