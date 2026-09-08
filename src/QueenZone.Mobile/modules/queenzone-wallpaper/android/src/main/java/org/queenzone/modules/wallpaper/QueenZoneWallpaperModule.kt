package org.queenzone.modules.wallpaper

import android.app.WallpaperManager
import android.net.Uri
import expo.modules.kotlin.modules.Module
import expo.modules.kotlin.modules.ModuleDefinition
import java.io.File
import java.io.FileInputStream

class QueenZoneWallpaperModule : Module() {
  override fun definition() = ModuleDefinition {
    Name("QueenZoneWallpaper")

    AsyncFunction("setWallpaper") { fileUri: String, target: String ->
      applyWallpaper(fileUri, target)
    }
  }

  private fun applyWallpaper(fileUri: String, target: String) {
    val context = appContext.reactContext ?: throw Exception("unsupported")
    val manager = WallpaperManager.getInstance(context)

    if (!manager.isWallpaperSupported || !manager.isSetWallpaperAllowed) {
      throw Exception("unsupported")
    }

    val flags = flagsFor(target)
    val file = fileFromUri(fileUri)
    FileInputStream(file).use { stream ->
      val written = manager.setStream(stream, null, true, flags)
      if (written and flags != flags) {
        throw Exception("target-failed")
      }
    }
  }

  private fun flagsFor(target: String): Int {
    return when (target) {
      "home" -> WallpaperManager.FLAG_SYSTEM
      "lock" -> WallpaperManager.FLAG_LOCK
      "both" -> WallpaperManager.FLAG_SYSTEM or WallpaperManager.FLAG_LOCK
      else -> throw Exception("target-failed")
    }
  }

  private fun fileFromUri(fileUri: String): File {
    val uri = Uri.parse(fileUri)
    val path = uri.path ?: throw Exception("target-failed")
    val file = File(path)
    if (!file.exists() || !file.canRead()) {
      throw Exception("target-failed")
    }
    return file
  }
}
