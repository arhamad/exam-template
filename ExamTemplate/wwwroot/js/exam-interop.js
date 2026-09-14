// Browser helpers for the exam editor: draft storage, downloads, PDF previews and focus.
// Loaded once per circuit by Services/BrowserInterop.cs.

const draftKey = 'examTemplate.draft.v1';
const draftInfoKey = 'examTemplate.draftInfo.v1';

// Values sent back to .NET travel over SignalR (32 KB per message), so free text is capped.
const maxInfoTextLength = 200;

export function saveDraft(json, info) {
    try {
        localStorage.setItem(draftKey, json);
        localStorage.setItem(draftInfoKey, JSON.stringify({
            courseName: info?.courseName ?? '',
            courseCode: info?.courseCode ?? '',
            questionCount: info?.questionCount ?? 0,
            savedAt: new Date().toISOString(),
        }));
        return true;
    } catch {
        // Quota exceeded or storage disabled; the previous draft is left as it was.
        return false;
    }
}

export function getDraftInfo(locale) {
    try {
        if (localStorage.getItem(draftKey) === null) {
            return null;
        }

        let info = {};
        try {
            info = JSON.parse(localStorage.getItem(draftInfoKey) ?? '{}') ?? {};
        } catch {
            info = {};
        }

        return {
            courseName: String(info.courseName ?? '').slice(0, maxInfoTextLength),
            courseCode: String(info.courseCode ?? '').slice(0, maxInfoTextLength),
            questionCount: Number.isInteger(info.questionCount) ? info.questionCount : 0,
            savedAtText: formatTime(info.savedAt, locale),
        };
    } catch {
        return null;
    }
}

// Returned as bytes so .NET reads it as a stream (IJSStreamReference) instead of one SignalR message.
// A stream reference can't be empty, so "no draft" is sent as a single space.
export function loadDraft() {
    let json = null;
    try {
        json = localStorage.getItem(draftKey);
    } catch {
        json = null;
    }

    return new TextEncoder().encode(json || ' ');
}

export function clearDraft() {
    try {
        localStorage.removeItem(draftKey);
        localStorage.removeItem(draftInfoKey);
        return true;
    } catch {
        return false;
    }
}

export async function downloadFromStream(fileName, contentType, streamRef) {
    const buffer = await streamRef.arrayBuffer();
    const url = URL.createObjectURL(new Blob([buffer], { type: contentType }));
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName ?? '';
    anchor.style.display = 'none';
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    // Revoked a moment later: some browsers start reading the blob only after click() returns.
    setTimeout(() => URL.revokeObjectURL(url), 10000);
}

export async function createObjectUrl(streamRef, contentType) {
    const buffer = await streamRef.arrayBuffer();
    return URL.createObjectURL(new Blob([buffer], { type: contentType }));
}

export function revokeObjectUrl(url) {
    if (typeof url === 'string' && url.startsWith('blob:')) {
        URL.revokeObjectURL(url);
    }
}

// Remembers the interface language for the server to read on the next page load. Only the two known codes are written.
export function setUiLanguage(code) {
    if (code !== 'ar' && code !== 'en') {
        return;
    }

    const secure = location.protocol === 'https:' ? '; secure' : '';
    document.cookie = `examTemplate.uiLanguage=${code}; path=/; max-age=31536000; samesite=lax${secure}`;
}

// alignToTop: deep-link targets are scrolled to the top; otherwise the browser scrolls only if needed.
export function focusElement(id, alignToTop) {
    const element = document.getElementById(id);
    if (!element) {
        return false;
    }

    if (alignToTop) {
        element.scrollIntoView({ block: 'start', behavior: 'instant' });
        element.focus({ preventScroll: true });
    } else {
        element.focus();
    }

    return document.activeElement === element;
}

function formatTime(isoText, locale) {
    if (typeof isoText !== 'string') {
        return '';
    }

    const date = new Date(isoText);
    if (Number.isNaN(date.getTime())) {
        return '';
    }

    // In the browser's own time zone. The .NET side asks for the Gregorian calendar and Western digits.
    return date.toLocaleString(typeof locale === 'string' ? locale : 'ar-u-ca-gregory-nu-latn', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
    });
}
