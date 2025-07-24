using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;
using System.IO;

namespace Musikspieler.Scripts.Audio
{
    public static class MP3Loader
    {
        public static AudioStreamWav Load(string path)
        {
            if (!File.Exists(path))
				return null;
            // Godot kann keine MP3 Dateien in WAV Dateien konvertieren, deswegen wird hier die MP3 Datei gar nicht erst geladen
            // Temporäre WAV-Datei erzeugen
            string tempWav = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg";
            ffmpeg.StartInfo.Arguments = $"-y -i \"{path}\" -ar 44100 -ac 2 -c:a pcm_s16le -f wav \"{tempWav}\"";
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.CreateNoWindow = true;
            // ffmpeg.StartInfo.RedirectStandardError = true;
            // ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.Start();
            ffmpeg.WaitForExit();

            // string ffmpegOut = ffmpeg.StandardOutput.ReadToEnd();
            // string ffmpegErr = ffmpeg.StandardError.ReadToEnd();
            // GD.Print("ffmpeg stdout:", ffmpegOut);
            // GD.Print("ffmpeg stderr:", ffmpegErr);

            if (ffmpeg.ExitCode != 0)
                throw new Exception($"ffmpeg failed with exit code {ffmpeg.ExitCode}");

            if (!File.Exists(tempWav))
                throw new FileNotFoundException($"Temp WAV file not found: {tempWav}");


            AudioStreamWav wav = AudioStreamWav.LoadFromFile(tempWav, new Dictionary{{"compress/mode",0}});
            File.Delete(tempWav);
            return wav;
        }
    }
}
