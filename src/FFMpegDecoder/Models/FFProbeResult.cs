using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace FFMpegDecoder.Models
{
    public class FFProbeResult
    {
        [JsonPropertyName("streams")]
        public List<FFProbeStream> Streams { get; set; } = new List<FFProbeStream>();

        [JsonPropertyName("format")]
        public FFProbeFormat? Format { get; set; }
    }

    public class FFProbeStreamDisposition
    {
        [JsonPropertyName("default")]
        public int? Default { get; set; }
        public int? dub { get; set; }
        public int? original { get; set; }
        public int? comment { get; set; }
        public int? lyrics { get; set; }
        public int? karaoke { get; set; }
        public int? forced { get; set; }
        public int? hearing_impaired { get; set; }
        public int? visual_impaired { get; set; }
        public int? clean_effects { get; set; }
        public int? attached_pic { get; set; }
        public int? timed_thumbnails { get; set; }
        public int? captions { get; set; }
        public int? descriptions { get; set; }
        public int? metadata { get; set; }
        public int? dependent { get; set; }
        public int? still_image { get; set; }
    }

    public class FFProbeFormat
    {
        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        [JsonPropertyName("nb_streams")]
        public int? StreamsNumber{ get; set; }

        [JsonPropertyName("nb_programs")]
        public int? ProgramsNumber { get; set; }

        [JsonPropertyName("format_name")]
        public string? FormatName { get; set; }

        [JsonPropertyName("format_long_name")]
        public string? FormatLongName { get; set; }

        [JsonPropertyName("start_time")]
        public string? StartTime { get; set; }

        [JsonPropertyName("probe_score")]
        public int? ProbeScore { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }

    public class FFProbeStream
    {
        public int? index { get; set; }
        public string? codec_name { get; set; }
        public string? codec_long_name { get; set; }
        public string? profile { get; set; }
        public string? codec_type { get; set; }
        public string? codec_tag_string { get; set; }
        public string? codec_tag { get; set; }
        public int? width { get; set; }
        public int? height { get; set; }
        public int? coded_width { get; set; }
        public int? coded_height { get; set; }
        public int? closed_captions { get; set; }
        public int? film_grain { get; set; }
        public int? has_b_frames { get; set; }
        public string? sample_aspect_ratio { get; set; }
        public string? display_aspect_ratio { get; set; }
        public string? pix_fmt { get; set; }
        public int? level { get; set; }
        public string? color_range { get; set; }
        public string? color_space { get; set; }
        public string? color_transfer { get; set; }
        public string? color_primaries { get; set; }
        public string? chroma_location { get; set; }
        public string? field_order { get; set; }
        public int? refs { get; set; }
        public string? is_avc { get; set; }
        public string? nal_length_size { get; set; }
        public string? id { get; set; }
        public string? r_frame_rate { get; set; }
        public string? avg_frame_rate { get; set; }
        public string? time_base { get; set; }
        public Int64? start_pts { get; set; }
        public string? start_time { get; set; }
        public string? bits_per_raw_sample { get; set; }
        public int? extradata_size { get; set; }
        public FFProbeStreamDisposition? disposition { get; set; }
        public string? sample_fmt { get; set; }
        public string? sample_rate { get; set; }
        public int? channels { get; set; }
        public string? channel_layout { get; set; }
        public int? bits_per_sample { get; set; }
        public int? initial_padding { get; set; }
        public string? bit_rate { get; set; }  

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }


}
