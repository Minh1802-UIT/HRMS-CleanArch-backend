using System;
using System.Linq;

namespace Employee.Application.Features.Attendance.Services
{
    /// <summary>
    /// Server-side face embedding comparison using cosine similarity.
    /// Embeddings are 128-float vectors extracted by face-api.js on the client.
    /// </summary>
    public class FaceVerificationService
    {
        /// <summary>
        /// Computes Euclidean distance between two face embeddings.
        /// This is the standard metric used by face-api.js.
        /// Lower is better. A typical threshold is 0.5 to 0.6.
        /// </summary>
        public double ComputeEuclideanDistance(float[] embedding1, float[] embedding2)
        {
            if (embedding1.Length != embedding2.Length)
                throw new ArgumentException("Embeddings must have the same dimension.");

            double sum = 0;
            for (int i = 0; i < embedding1.Length; i++)
            {
                var diff = embedding1[i] - embedding2[i];
                sum += diff * diff;
            }

            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Verifies if a check-in embedding matches the registered embedding.
        /// Uses 0.55 as the strict threshold for Euclidean distance.
        /// </summary>
        public FaceMatchResult Verify(float[] checkInEmbedding, float[] registeredEmbedding, double threshold = 0.55)
        {
            var distance = ComputeEuclideanDistance(checkInEmbedding, registeredEmbedding);
            
            // Map Euclidean distance to an intuitive Similarity percentage (0 to 1) 
            // - We want exact match (0.0) -> 1.0 (100%)
            // - Threshold (0.55) -> 0.80 (80%)
            // - Max expected distance (1.2) -> 0.0 (0%)
            double similarity;
            if (distance <= threshold)
            {
                similarity = 1.0 - (distance / threshold) * 0.20; 
            }
            else
            {
                // Gradually drop from 80% to 0% as distance grows from 0.55 to 1.2
                similarity = Math.Max(0.0, 0.80 - ((distance - threshold) / (1.2 - threshold)) * 0.80);
            }

            return new FaceMatchResult
            {
                Similarity = Math.Round(similarity, 4),
                IsMatch = distance <= threshold,
                Threshold = threshold
            };
        }
    }

    public class FaceMatchResult
    {
        public double Similarity { get; set; }
        public bool IsMatch { get; set; }
        public double Threshold { get; set; }
    }
}
