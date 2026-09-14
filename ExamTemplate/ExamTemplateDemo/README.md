# ExamTemplateDemo

مشروع Console بسيط على .NET 8 لاختبار توليد نموذج امتحان Word عربي ديناميكي باستخدام **TriasDev.Templify 1.7.0**.

## ما الذي يختبره المثال؟

- عدد أسئلة متغير.
- السؤال قد يكون `MultipleChoice` أو `Essay`.
- عدد خيارات الاختيار من متعدد متغير.
- حجم مساحة الحل للسؤال المقالي متغير عن طريق `AnswerLines`.
- عنوان قسم وتعليماته اختياريان لكل سؤال.
- العلامة لكل سؤال.
- نسخة طالب ونسخة مفتاح إجابة من **نفس القالب**.
- Footer فيه `PAGE / NUMPAGES` ويتحدث عند فتح Word.
- RTL عربي، وتنسيق طباعة أبيض/رمادي لتقليل استهلاك الحبر.

## التشغيل

```bash
dotnet restore
dotnet run
```

الناتج يظهر تحت:

```text
bin/Debug/net8.0/Output/Exam-Student.docx
bin/Debug/net8.0/Output/Exam-AnswerKey.docx
```

## أين أغير البيانات؟

عدّل `DemoData.cs`. أهم الخصائص:

```csharp
Type = QuestionType.MultipleChoice
Options = [...]

Type = QuestionType.Essay
AnswerLines = 8
ModelAnswer = "..."
```

`AnswerLines` هو التحكم المباشر بارتفاع مساحة الحل: 3 لسؤال قصير، 8 أو 10 لسؤال يحتاج خطوات، وهكذا. تم وضع حد أقصى 20 سطرًا في الـMapper لمنع إنشاء مستندات ضخمة بالخطأ.

## القالب

`Templates/ExamTemplate.docx` يحتوي على منطق Templify مثل:

```text
{{#foreach q in Questions}}
{{#if q.Type = "MultipleChoice"}}
{{q.OptionsBlock}}
{{/if}}

{{#if q.Type = "Essay"}}
{{#if ShowAnswers}}
{{q.ModelAnswer}}
{{#else}}
{{q.AnswerBlock}}
{{/if}}
{{/if}}
{{/foreach}}
```

لا تعدّل الأقواس أو تقسّم placeholder على أكثر من تنسيق داخل Word. يمكنك تغيير الخط، المسافات، الحدود، أحجام النص والخلفيات بحرية.

## ملاحظات تصميم

1. معلومات المقرر والطالب في Header بصري مدمج من ثلاث مناطق مثل النموذج الحالي، لكن بفواصل أخف ومساحات أكثر انتظامًا.
2. التعليمات داخل صندوق مستقل مع العلامة الكلية.
3. كل سؤال يبدأ بشريط واضح: رقم السؤال + النوع + العلامة.
4. سؤال الحل لا يستخدم جداول فارغة كثيرة؛ مساحة الحل تُولد كسطور بحسب `AnswerLines` لتبقى المرونة في البيانات لا في القالب.
5. نفس القالب يولد Answer Key عن طريق `ShowAnswers=true`.
6. للحصول على pagination ثابت على Linux/Docker ثبت نفس الخط العربي المستخدم في القالب على السيرفر.

## نقل الفكرة إلى Blazor

اترك `ExamDocumentGenerator` و`ExamTemplateMapper` كما هما وانقلهما إلى مشروع ASP.NET Core، ثم أرجع `MemoryStream` أو `byte[]` من endpoint / service بدل الكتابة إلى ملف. من الأفضل إبقاء توليد Word على الخادم وليس داخل Blazor WebAssembly.

## الملفات Samples

- `Samples/Exam-Student-Preview.docx`: معاينة شكل نسخة الطالب.
- `Samples/Exam-AnswerKey-Preview.docx`: معاينة مفتاح الإجابة.

> ملاحظة: تم إنشاء ملفات الـPreview لعرض التصميم بصريًا في هذه الحزمة. التشغيل الفعلي للمشروع سيولد الملفات من `ExamTemplate.docx` عبر Templify.
