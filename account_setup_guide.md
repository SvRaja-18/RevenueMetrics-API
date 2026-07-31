# Account Setup & API Key Guide

This guide summarizes exactly how to create the free accounts and retrieve the required API credentials for the `RevenueMetrics` sync pipeline.

---

## 1. Supabase (PostgreSQL Database)
We need a free PostgreSQL database to store the normalized data and sync cursors.

1. Go to [Supabase](https://supabase.com/) and click **Start your project**.
2. Sign in with GitHub or create an account.
3. Click **New Project** and select an organization.
4. Name the project (e.g., `RevenueMetrics DB`) and generate a secure database password. (Save this password!).
5. Once the project provisions (takes ~2 minutes), go to the **Project Settings** (gear icon on the left) ➔ **Database**.
6. Scroll down to **Connection string** and select **URI**. 
7. Copy the string. It will look like: `postgresql://postgres.[YOUR-PROJECT-REF]:[YOUR-PASSWORD]@aws-0-....pooler.supabase.com:6543/postgres`. 
8. Replace `[YOUR-PASSWORD]` with the password you created in step 4. This is your `ConnectionStrings__Supabase`.

---

## 2. HubSpot (CRM Source)
The assignment requires a developer test account to generate dummy CRM data.

1. Go to the [HubSpot Developer Platform](https://developers.hubspot.com/) and click **Create a free developer account**. (Do not use the standard company signup).
2. Once logged into the developer portal, go to **Testing** ➔ **Test Accounts** and click **Create developer test account**. Name it "RevenueMetrics Test".
3. Open the newly created test account. 
4. **Create Sample Data**: Go to **CRM ➔ Contacts/Deals** and manually add 2-3 sample deals.
5. **Get the API Key**: 
   - Inside the test account, click the **Settings** gear icon (top right).
   - In the left sidebar, navigate to **Integrations** ➔ **Private Apps**.
   - Click **Create a private app**, name it "Sync Pipeline".
   - Go to the **Scopes** tab and check the `crm.objects.deals.read` (or similar CRM read permissions).
   - Click **Create app**. 
6. Click **Show Token** and copy it. This is your `HubSpot__PrivateAppToken` (starts with `pat-na...`).

---

## 3. Stripe (Payments Source)
We need a standard test environment key from Stripe.

1. Go to [Stripe](https://stripe.com/) and create a free account.
2. Once logged in, ensure **Test Mode** is toggled ON (usually in the top right).
3. In the dashboard, click on **Developers** (top right) ➔ **API keys**.
4. Under Standard keys, look for the **Secret key**.
5. Click **Reveal test key**. It will start with `sk_test_...`.
6. Copy this key. This is your `Stripe__SecretKey`.

---

## 4. Google Calendar (Events Source)
Since accessing a private calendar requires user consent, we must use OAuth 2.0 rather than a simple API key.

1. Go to the [Google Cloud Console](https://console.cloud.google.com/) and sign in.
2. Click the project dropdown at the top and click **New Project**. Name it `RevenueMetrics-Calendar`.
3. Go to **APIs & Services** ➔ **Library**. Search for **Google Calendar API** and click **Enable**.
4. Go to **APIs & Services** ➔ **OAuth consent screen**.
   - Select **External** and click Create.
   - Fill in mandatory fields (App name, support email) and save. (You do not need to publish the app, leave it in Testing mode).
5. Go to **APIs & Services** ➔ **Credentials**.
   - Click **+ Create Credentials** ➔ **OAuth client ID**.
   - Application type: **Desktop app**. Name it "Calendar Sync".
6. A popup will appear. Click **Download JSON**.
7. Rename the downloaded file exactly to **`credentials.json`** and place it in the root folder of your local repository. 
*(Note: Because we added it to `.gitignore`, this file will safely stay on your computer and won't be pushed to GitHub).*
