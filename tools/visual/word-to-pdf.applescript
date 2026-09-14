-- Converts a .docx to PDF with Microsoft Word for Mac (used to check Word's own rendering).
-- Usage: osascript tools/visual/word-to-pdf.applescript <input.docx> <output.pdf>
-- Tip: keep files inside ~/Library/Containers/com.microsoft.Word/Data/ to avoid Word's file-access prompt.
on run argv
	if (count of argv) < 2 then error "usage: osascript word-to-pdf.applescript <input.docx> <output.pdf>"
	set inputPath to item 1 of argv
	set outputPath to item 2 of argv
	tell application "Microsoft Word"
		open (POSIX file inputPath)
		set theDocument to active document
		save as theDocument file name outputPath file format format PDF
		close theDocument saving no
	end tell
end run
