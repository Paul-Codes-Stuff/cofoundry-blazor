// Reads the emails the sandbox drops into its mail folder (the default LocalDrop send mode), so the
// steps that create users can read temporary passwords and links. Paths are relative to the repo root.
const fs = require('fs');
const path = require('path');

const MAIL_DIR = path.resolve('samples/src/Dev/Dev.Sandbox/App_Data/Emails');

// The newest dropped email to `to` whose subject matches, as text.
const readMail = async (to, subject, since) => {
  for (let i = 0; i < 30; i++) {
    const files = fs.readdirSync(MAIL_DIR)
      .map(f => path.join(MAIL_DIR, f))
      .filter(f => fs.statSync(f).mtimeMs >= since)
      .sort((a, b) => fs.statSync(b).mtimeMs - fs.statSync(a).mtimeMs);
    for (const f of files) {
      const text = fs.readFileSync(f, 'utf8');
      if (text.includes('To: ' + to) && subject.test(text.match(/^Subject: (.*)$/m)?.[1] || '')) return text;
    }
    await new Promise(r => setTimeout(r, 500));
  }
  throw new Error(`no "${subject}" email to ${to}`);
};

module.exports = { readMail };
