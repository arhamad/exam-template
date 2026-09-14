// Rasterizes every page of a PDF to PNG files using PDFKit (no third-party tools needed).
// Usage: swift tools/visual/pdf-to-png.swift <input.pdf> <outputDir> [dpi=144]
import AppKit
import Foundation
import PDFKit

let arguments = CommandLine.arguments
guard arguments.count >= 3 else {
    FileHandle.standardError.write(Data("usage: pdf-to-png.swift <input.pdf> <outputDir> [dpi]\n".utf8))
    exit(2)
}

let input = URL(fileURLWithPath: arguments[1])
let outputDirectory = URL(fileURLWithPath: arguments[2], isDirectory: true)
let dpi = arguments.count > 3 ? (Double(arguments[3]) ?? 144) : 144

guard let document = PDFDocument(url: input) else {
    FileHandle.standardError.write(Data("cannot open \(input.path)\n".utf8))
    exit(1)
}

try FileManager.default.createDirectory(at: outputDirectory, withIntermediateDirectories: true)
let scale = dpi / 72.0
let baseName = input.deletingPathExtension().lastPathComponent

for index in 0..<document.pageCount {
    guard let page = document.page(at: index) else { continue }
    let bounds = page.bounds(for: .mediaBox)
    let width = Int((bounds.width * scale).rounded())
    let height = Int((bounds.height * scale).rounded())

    guard let context = CGContext(
        data: nil, width: width, height: height, bitsPerComponent: 8, bytesPerRow: 0,
        space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.noneSkipLast.rawValue)
    else { continue }

    context.setFillColor(CGColor(red: 1, green: 1, blue: 1, alpha: 1))
    context.fill(CGRect(x: 0, y: 0, width: width, height: height))
    context.scaleBy(x: scale, y: scale)
    context.translateBy(x: -bounds.origin.x, y: -bounds.origin.y)
    page.draw(with: .mediaBox, to: context)

    guard let image = context.makeImage(),
          let png = NSBitmapImageRep(cgImage: image).representation(using: .png, properties: [:])
    else { continue }

    let output = outputDirectory.appendingPathComponent(String(format: "%@-p%02d.png", baseName, index + 1))
    try png.write(to: output)
    print(output.path)
}
