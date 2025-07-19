using Godot;
using System;
using System.Diagnostics;
using System.IO;

namespace Musikspieler.Scripts.Audio
{
    public static class MP3Loader
    {
        public static AudioStreamWav Load(string path)
        {
            // Godot kann keine MP3 Dateien in WAV Dateien konvertieren, deswegen wird hier die MP3 Datei gar nicht erst geladen
            // Temporäre WAV-Datei erzeugen
            string tempWav = Path.GetTempFileName() + ".wav";
            var ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg";
            ffmpeg.StartInfo.Arguments = $"-y -i \"{path}\" -ar 44100 -ac 2 -f wav \"{tempWav}\"";
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.Start();
            ffmpeg.WaitForExit();

            // WAV-Datei laden
            var wav = new AudioStreamWav();
            wav.Data = File.ReadAllBytes(tempWav);
            File.Delete(tempWav);
            return wav;
        }
    }
}
