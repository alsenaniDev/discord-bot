# Game Plugin Architecture

## لماذا نفصل الألعاب؟

إضافة لعبة كبيرة مثل الروليت داخل نفس مسارات الـ API والـ Activity مباشرة يجعل كل تطوير جديد يلمس أجزاء كثيرة من المنصة. الهدف من هذه الطبقة هو تحويل الألعاب تدريجيًا إلى مكونات لها عقد واضح مع المنصة، بدون كسر Quiz أو Roulette الحاليين.

في هذه المرحلة لا ننقل الألعاب إلى مستودعات منفصلة، ولا نعيد كتابة الروليت. نضيف أساسًا قابلًا للتوسع بجانب النظام الحالي.

## أنواع تشغيل الألعاب

### Platform

اللعبة تعمل بالكامل داخل المنصة:

- Frontend داخل Discord Activity.
- Backend داخل API الحالي.
- قواعد النقاط والمحفظة والنشر تتحكم بها المنصة.

مثال حالي: Quiz.

### Hybrid

اللعبة لها أجزاء خاصة بها، لكنها تستخدم خدمات المنصة:

- Activity route داخلي أو frontend مستقل لاحقًا.
- Backend داخلي أو خارجي لاحقًا.
- المنصة توفر التوثيق، السيرفر، الروم، الخطة، المحفظة، الأحداث، والنشر.

مثال حالي مناسب: Roulette.

### External

اللعبة قد تكون مستضافة خارج المنصة مستقبلًا، لكنها لا تثق بأي Discord IDs من المتصفح. يجب أن تستخدم `GameRuntimeToken` أو توقيع server-to-server.

## Game Manifest

كل إصدار لعبة يملك `ManifestJson` مخزنًا في جدول `GameVersions`.

الحقول الأساسية:

```json
{
  "key": "roulette",
  "name": "الروليت",
  "description": "لعبة جماعية تعتمد على الحظ والتحدي بين الأعضاء.",
  "playMode": "Multiplayer",
  "engineType": "Hybrid",
  "frontendMode": "InternalRoute",
  "activityRoute": "/games/roulette",
  "frontendUrl": null,
  "backendUrl": null,
  "requiredPlan": "pro",
  "supportsWallet": true,
  "supportsLeaderboard": true,
  "supportsPowerUps": true,
  "supportsBotPublishing": true,
  "events": ["roulette.room.created", "roulette.room.completed", "roulette.player.won"],
  "permissions": ["wallet.read", "wallet.transaction.request", "bot.publish.request"],
  "sandboxAllowedOrigins": [],
  "configSchema": {}
}
```

القيم الداخلية تبقى بالإنجليزية. أي نص يظهر للمستخدم في Discord أو الداشبورد يكون بالعربية.

## Game Versions

جدول `GameVersions` يسمح بأكثر من إصدار لكل لعبة.

الحالات:

- `Draft` — مسودة
- `Sandbox` — تجريبية
- `InReview` — قيد المراجعة
- `Published` — منشورة
- `Rejected` — مرفوضة
- `Disabled` — معطلة

قاعدة مهمة: عند نشر إصدار جديد، يتم تعطيل أي إصدار منشور آخر لنفس اللعبة. هذا يحافظ على إصدار إنتاج واحد فقط.

تمت إضافة إصدارات أولية منشورة لـ:

- Quiz `1.0.0`
- Roulette `1.0.0`

## Sandbox workflow

جدول `GameSandboxAccess` يحدد من يرى نسخة Sandbox.

القاعدة:

- المستخدم العادي يرى الإصدار `Published`.
- إذا كان السيرفر/المستخدم موجودًا في `GameSandboxAccess` لإصدار Sandbox، تعرض الـ Activity النسخة التجريبية بدل المنشورة.
- تظهر badge: `تجريبية`.
- تظهر رسالة: `هذه نسخة تجريبية وقد تحتوي على أخطاء.`

## Runtime Token

endpoint:

```http
POST /api/games/runtime/token
```

الطلب:

```json
{
  "gameKey": "roulette",
  "guildDiscordId": "123",
  "channelDiscordId": "456"
}
```

الاستجابة:

```json
{
  "runtimeToken": "grt_...",
  "expiresAt": "2026-07-07T12:00:00Z",
  "mode": "Production",
  "gameVersionId": "..."
}
```

المنصة تصدر الرمز فقط بعد التحقق من:

- Discord Activity bearer token.
- السيرفر مربوط ونشط.
- الألعاب مفعلة.
- الروم هو روم الألعاب.
- اللعبة مفعلة للسيرفر.
- الخطة تسمح باللعبة.
- Sandbox access إذا الإصدار تجريبي.

الرمز يخزن كـ hash في قاعدة البيانات، وصلاحيته قصيرة. لا يتم كشف أسرار المنصة للعبة.

## Platform Integration APIs

كل endpoint تحت:

```http
/api/game-integrations
```

يتطلب:

```http
Authorization: Bearer {GameRuntimeToken}
```

المسارات الحالية:

- `GET /api/game-integrations/me`
- `GET /api/game-integrations/wallet`
- `POST /api/game-integrations/wallet/transactions`
- `POST /api/game-integrations/events`
- `POST /api/game-integrations/bot/publish`

## قواعد المحفظة

اللعبة لا تستطيع إضافة عملات مباشرة.

المسموح في foundation الحالي:

- قراءة الرصيد.
- طلب خصم عملات بقيمة سالبة مع idempotency key.

إضافة المكافآت يجب أن تتم عبر أحداث لعبة تخضع لقواعد المنصة، وليس بطلب مباشر من frontend خارجي.

## Game Events

جدول `GameEvents` يمثل أحداثًا عامة:

- `roulette.room.created`
- `roulette.room.completed`
- `quiz.completed`
- `store.purchase`
- `game.result.publish_requested`

كل حدث يتطلب `IdempotencyKey` لمنع التكرار.

الحالة:

- `Pending`
- `Processed`
- `Failed`

## Bot Publishing Contract

جدول `GameBotPublishActions` يمثل طلب نشر عام للـ bot.

`MessageJson` يدعم:

- `title`
- `description`
- `fields`
- `buttons`
- `imageUrl`
- `footer`

Button action types المستقبلية:

- `launch_activity`
- `join_game_room`
- `open_leaderboard`

endpoint للبوت:

- `GET /api/bot/games/generic-publish-actions/pending`
- `POST /api/bot/games/generic-publish-actions/{id}/ack`

ملاحظة: نشر الروليت الحالي لم يتم حذفه. العقد العام أضيف بجانبه.

## Platform Admin basics

لوحة كتالوج الألعاب تعرض الآن:

- إصدارات اللعبة
- إنشاء نسخة تجريبية
- نشر الإصدار
- تعطيل الإصدار
- إضافة سيرفرات الاختبار
- حذف صلاحية اختبار
- عرض Manifest JSON

هذه واجهة foundation، ويمكن لاحقًا تحسينها إلى صفحة مستقلة للإصدارات مع محرر JSON مخصص ومراجعة publish workflow.

## قواعد الأمان

- لا تقبل المنصة Discord IDs من frontend خارجي كحقيقة.
- لا يسمح بروابط خارجية في الإنتاج بدون مراجعة.
- لا يسمح بتعديل المحفظة مباشرة من اللعبة.
- لا يسمح للعبة بالنشر المباشر إلى Discord.
- كل عملية مؤثرة يجب أن تكون idempotent.
- أي Sandbox يجب أن يكون محصورًا في test guild/user.
- لا real money ولا cash-out.

## غير مستهدف في هذه المرحلة

- نقل Roulette إلى خدمة منفصلة.
- فتح public developer submissions.
- تشغيل iframe خارجي غير مراجع في الإنتاج.
- استبدال APIs الحالية.
- حذف bot publishing الحالي.

## ملاحظات اختبار يدوية

1. افتح Admin Games.
2. اختر لعبة واضغط `إصدارات اللعبة`.
3. أنشئ نسخة تجريبية.
4. أضف Discord Guild ID لسيرفر اختبار.
5. افتح Activity من نفس السيرفر.
6. تأكد أن اللعبة تظهر badge `تجريبية`.
7. جرّب `POST /api/games/runtime/token` من Activity bearer token.
8. استخدم runtime token مع `GET /api/game-integrations/me`.
9. جرّب event بنفس `IdempotencyKey` مرتين وتأكد أنه لا ينشئ حدثين.
