# Heroic Importer

This is a very quick "vibe coding" experiment to import the JSON files that are from Heroic Games Launcher into a database.  

I used GitHub Copilot to do a lot of the work, but I cleaned it up and sanity checked it myself.  I gave a lot of advice to the AI, and it worked well with me.

Note: it imports things like runtimes and editors into the DB, but these things don't show up in the Heroic games list.  That probably explains why the Heroic total games list is different from the record count.

Still to consider: 

- Auto-creating the SQLite database if it doesn't exist
- I added a "hidden" flag to the DB manually for the ones above.  Maybe auto-create that column as well.
- Soemthing else

This is probably fine.  It took a very short while to get it working like this, and I think I'm fairly happy with it... for now.
