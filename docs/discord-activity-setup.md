# إعداد Discord Activity

تطبيق الـ Activity موجود في `activity/DiscordBot.Activity` ويعمل بواجهة عربية داخل Discord. لا تضع `Client Secret` أو مفتاح البوت في مشروع الواجهة.

## إعداد Discord Developer Portal

1. افتح [Discord Developer Portal](https://discord.com/developers/applications) واختر نفس التطبيق المستخدم للبوت.
2. من **Activities → Settings** فعّل **Enable Activities** وحدد المنصات المدعومة.
3. من **OAuth2** أضف Redirect URI مثل `https://127.0.0.1`. يعالج Embedded App SDK الرجوع إلى الـ Activity تلقائيًا.
4. من **Activities → URL Mappings** أضف بالترتيب:

   | Prefix | Target |
   |---|---|
   | `/api` | `YOUR_API_DOMAIN/api` |
   | `/` | `YOUR_ACTIVITY_FRONTEND_DOMAIN` |

   اكتب Target من دون `https://`. ضع المسار الأطول `/api` قبل `/` حتى لا يلتقط مسار الواجهة طلبات API.
5. تأكد أن التطبيق يملك أمر Entry Point الافتراضي وأن البوت مثبت بصلاحية `applications.commands`.

Discord يشغّل Activities داخل iframe معزول ويمرر الطلبات الخارجية عبر proxy؛ لذلك URL Mappings ضرورية للإنتاج والتطوير عبر tunnel.

المراجع الرسمية: [بدء بناء Activity](https://docs.discord.com/developers/activities/building-an-activity)، [URL Mappings والتطوير المحلي](https://docs.discord.com/developers/activities/development-guides/local-development)، و[إطلاق Activity من interaction](https://docs.discord.com/developers/activities/how-activities-work).

## متغيرات البيئة

واجهة Activity وقت البناء:

```env
VITE_DISCORD_CLIENT_ID=YOUR_DISCORD_CLIENT_ID
VITE_API_BASE_URL=
```

اترك `VITE_API_BASE_URL` فارغًا عند استخدام mapping `/api`. للتطوير المباشر يمكن وضع رابط API الكامل مثل `https://api-tunnel.example.com`.

API:

```env
Discord__ClientId=YOUR_DISCORD_CLIENT_ID
Discord__ClientSecret=YOUR_DISCORD_CLIENT_SECRET
Discord__ActivityUrl=https://YOUR_DISCORD_CLIENT_ID.discordsays.com
```

يبقى `Discord__ClientSecret` في خدمة API فقط. لا يوضع في Vite أو Vercel كمتغير يبدأ بـ `VITE_`.

## التشغيل والبناء

```bash
cd activity/DiscordBot.Activity
npm install
npm run dev
npm run build
```

انشر مجلد `dist` كـ SPA واضبط rewrite لكل المسارات إلى `index.html`. ملف `vercel.json` مضاف لهذا الغرض.

للتطوير عبر Discord يفضّل استخدام tunnel HTTPS للواجهة والـ API، ثم تحديث URL Mappings إلى نطاقات الـ tunnel. لا تستخدم أسرار الإنتاج محليًا.

## قائمة الاختبار اليدوي

- [ ] تشغيل `/games` خارج روم الألعاب يعرض رسالة عربية تحدد أن الألعاب متاحة في الروم المخصص فقط.
- [ ] تشغيل `/games` داخل روم الألعاب يفتح الـ Activity.
- [ ] تظهر شاشة `🎮 مركز الألعاب` ولعبة `تحدي الأسئلة`.
- [ ] بدء Quiz ينشئ `GameSession` بحالة `Started`.
- [ ] الفوز بإجابتين أو أكثر يكمل الجلسة ويضيف نقاط الفوز ويحدث `GamePlayer`.
- [ ] الخسارة تكمل الجلسة من دون نقاط فوز.
- [ ] محاولة إكمال نفس `sessionId` مرتين تُرفض من API.
- [ ] فتح الواجهة في سيرفر غير مربوط يعرض `هذا السيرفر غير مربوط بمنصة البوت.`
- [ ] فتحها في روم خاطئ يعرض رسالة روم الألعاب العربية.
- [ ] انتهاء مهلة الجلسة يعرض `انتهت جلسة اللعبة. ابدأ من جديد.`
- [ ] شاشة الترتيب تعرض النقاط والانتصارات وعدد الألعاب، أو حالة فارغة عربية.
- [ ] نشر النتيجة يحدث في روم الألعاب فقط وعند تفعيل إعداد النشر.
- [ ] تعطيل اللعبة أو تغيير الباقة يمنع بدء جلسة جديدة.
- [ ] عند تعطيل Activities في Portal يفشل الإطلاق بأمان ويظهر تدفق أزرار Games Hub القديم.

## ملاحظات المرحلة الحالية

- Quiz محلي وثابت؛ يمكن نقله لاحقًا إلى `GameContent`.
- لا يوجد multiplayer في هذه المرحلة.
- لا توجد ألعاب أو روابط خارجية يضيفها صاحب السيرفر.
- access token محفوظ في ذاكرة صفحة الـ Activity فقط، وتتحقق منه خدمة API عبر Discord `/users/@me`.
