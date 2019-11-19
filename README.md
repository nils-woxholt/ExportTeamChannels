# ExportTeamChannels
### Export all channels, posts and replies from a Microsoft Teams team!

This is a project that I made to export a team and its messages from Microsoft Teams.   
I needed to save an offline copy of all the messages and replies following a hackathon that I supported.  
We had extensively used teams to provide support for remote participants and felt that there was valuable information shared in the team that might be helpful to others going forward.

# What does it do?
This project uses the Microsoft Graph API to export the data.

With the data, the application creates 2 output folders: 1 for channels and 1 for messages.

Then for each channel and message a JSON file is created with the raw data (ue these files if you want to import the data into another system).

The application also creates markdown files for each channnel and message.
These files are used as collections in Jekyll to create a static website from the data.

- Jekyll: https://jekyllrb.com/
- Jekyll collections: https://jekyllrb.com/docs/collections/

# Features
- get a list of channels in the team 
- for each channel get all messages
- for each message get all replies
- download images from messages and change the source of the image (which points back to Teams) to an inline base64 encoded copy of the image.

# Usage
This is a console app

Run the app with 2 command line paramaters:

-t <teamid> This is the team id of the Team that you want to export

-b <bearertoken> The bearer token for the graph api 
  (get this by logging in using the Microsoft Graph Explorer)

# Microsoft Graph Explorer

https://developer.microsoft.com/en-us/graph/graph-explorer

### get the teamid
- Login to access your own data
- In the Sample Queries section on the left, click "*show more samples*"
- activate the "*Microsoft Teams*" collection of sample queries
- run the "*my joined teams*" query, view the results and copy the id value of the team that you want to export

### get the bearertoken
- Login to access your own data
- click the "*Preview*" toggle to enter the preview website
- in the main section of the screen, click the "*Auth*" tab in the top panel
- copy the access token


