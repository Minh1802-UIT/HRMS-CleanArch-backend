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
        /// Uses 0.52 as the strict threshold for Euclidean distance to balance False Accept/False Reject.
        /// </summary>
        public FaceMatchResult Verify(float[] checkInEmbedding, float[] registeredEmbedding, double threshold = 0.52)
        {
            var distance = ComputeEuclideanDistance(checkInEmbedding, registeredEmbedding);
            
            // Map Euclidean distance to Cosine Similarity percentage (assuming normalized vectors)
            // Distance^2 = 2 - 2 * Cosine(theta)  => Cosine Similarity = 1 - (Distance^2 / 2)
            double similarity = 1.0 - ((distance * distance) / 2.0);
            
            // Floor negative similarities to 0
            similarity = Math.Max(0.0, similarity);

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
