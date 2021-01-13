using Google.Cloud.Vision.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wheeler.PictureAnalyzer
{
    public class VisionAnalysis
    {
        public class Label
        {
            public Likelihood Likelihood { get; set; }
            public string Name { get; set; }
            public float Score { get; set; }

            public override string ToString()
            {
                string likelihoodString = Likelihood.Unknown != Likelihood ? $"[{Likelihood}]" : "";
                string scoreString = Score > 0 ? $"[{Score}]" : "";

                return $"{Name}{scoreString}{likelihoodString}";
            }
        }

        public List<Label> Labels { get; set; }


        public VisionAnalysis(SafeSearchAnnotation safeSearch, IReadOnlyList<EntityAnnotation> labels)
        {
            // add safe search labels
            Labels = new List<Label>
            {
                new Label()
                {
                    Likelihood = safeSearch.Adult,
                    Name = "Adult",
                    Score = safeSearch.AdultConfidence
                },
                new Label()
                {
                    Likelihood = safeSearch.Medical,
                    Name = "Medical",
                    Score = safeSearch.MedicalConfidence
                },
                new Label()
                {
                    Likelihood = safeSearch.Racy,
                    Name = "Racy",
                    Score = safeSearch.RacyConfidence
                },
                new Label()
                {
                    Likelihood = safeSearch.Spoof,
                    Name = "Spoof",
                    Score = safeSearch.SpoofConfidence
                },
                new Label()
                {
                    Likelihood = safeSearch.Violence,
                    Name = "Violence",
                    Score = safeSearch.ViolenceConfidence
                }
            };

            // add nsfw label
            Likelihood nsfw = Labels.Select(i => i.Likelihood).Max();
            Labels.Add(new Label()
            {
                Likelihood = nsfw,
                Name = "Nsfw",
                Score = safeSearch.NsfwConfidence
            });

            // add other labels
            Labels.AddRange(labels.Select(i => new Label()
            {
                Likelihood = Likelihood.Unknown,
                Name = i.Description,
                Score = i.Score
            }));
        }

        public override string ToString()
        {
            return string.Join("\n", Labels.Select(i => $"Label : {i}"));
        }
    }
}
