# Beta Tester Guide

Welcome to the Discord Bot Platform beta. This guide walks you through every feature we want tested before public launch.

**You will need:**

- A Discord account
- A Discord server where you have **Manage Server** permission (to invite the bot)
- The beta dashboard URL (provided by the team)
- The bot invited to your server

---

## 1. Log in with Discord

1. Open the beta dashboard URL in your browser (Chrome or Firefox recommended).
2. Click **Login with Discord**.
3. Authorize the application when Discord asks.
4. You should land back on the dashboard **Servers** page.

**If login fails:**

- Check that you are not blocking pop-ups or third-party cookies.
- Report the exact error message and screenshot to the team.

---

## 2. Invite the bot

1. On the **Servers** page, click **Invite Bot** (or use the onboarding invite button).
2. Select your test server and authorize the bot.
3. Confirm the bot appears in your server’s member list (should show **Online**).

---

## 3. Run `/setup` in Discord

1. In your Discord server, type `/setup` and run the command.
2. The bot should reply with a setup embed and a link to the dashboard.
3. Return to the dashboard and click **Refresh** (or reload the page).
4. Your server should now appear in the server list.

Open the server → **Overview** and confirm the setup checklist shows progress.

---

## 4. Configure modules

1. Open your server → **Modules**.
2. Turn on the features you want to test (Welcome, Logs, Tickets, Moderation, Reaction Roles, etc.).
3. Confirm toggles save without errors.
4. If a module is **Locked by plan**, note which plan is required — that is expected for some beta tiers.

---

## 5. Configure welcome messages

1. Open **Settings**.
2. Click **Sync Discord Data** if channel dropdowns are empty (wait a few seconds, then refresh the page if needed).
3. Enable **Welcome messages**.
4. Select a **Welcome channel** and edit the message (you can use `{user}` and `{server}` placeholders).
5. Click **Save changes**.
6. Join the server with a second Discord account (or ask a friend) and confirm a welcome message is posted.

**Note:** The bot needs **Server Members Intent** enabled and must be online.

---

## 6. Create a ticket

1. In Discord, run `/ticket setup` and pick a category (if not already configured in Settings).
2. Run `/ticket open` to open a support ticket.
3. In the dashboard, open **Tickets** and confirm the ticket appears.
4. Close the ticket in Discord with `/ticket close` and confirm status updates in the dashboard.

---

## 7. Create a reaction role

1. In Discord, run `/reaction-role create` and follow the prompts (channel, role, title).
2. Click the button on the panel to assign yourself the role.
3. In the dashboard, open **Reaction Roles** and confirm the panel is listed.
4. Optional: deactivate the panel from the dashboard and confirm it updates.

---

## 8. Test moderation

1. Ensure the **Moderation** module is enabled.
2. In Discord, test at least one command:
   - `/warn @user reason`
   - `/kick @user reason` (use a test account)
   - `/clear 5` (in a test channel)
3. Open **Moderation** in the dashboard and confirm warnings/cases appear.
4. Use filters (date, type) and confirm results update.

---

## 9. Check logs

1. After performing the steps above, open **Logs** in the dashboard.
2. Confirm events appear (member joined, ticket opened, moderation actions, etc.).
3. Try filtering by event type and search text.

---

## 10. Switch language (English / Arabic)

1. In the dashboard top bar, open the **language switcher** (globe icon).
2. Switch to **Arabic (العربية)**.
3. Confirm:
   - Text is translated
   - Layout flows right-to-left (sidebar, forms, tables)
   - Navigation and buttons still work
4. Switch back to **English** and confirm everything returns to normal.

---

## What to report

Please send the team:

| Item | Details |
|------|---------|
| What you tried | Step number and feature |
| Expected | What should have happened |
| Actual | What happened instead |
| Screenshots | Dashboard errors, Discord messages, browser console (F12) |
| Server | Discord server name (not IDs unless asked) |
| Language | English or Arabic when issue occurred |

---

## Quick checklist

- [ ] Login with Discord
- [ ] Bot invited and online
- [ ] `/setup` completed
- [ ] Modules configured
- [ ] Welcome message works
- [ ] Ticket open/close works
- [ ] Reaction role works
- [ ] Moderation command works
- [ ] Logs show activity
- [ ] Arabic RTL works

Thank you for helping test the beta.
