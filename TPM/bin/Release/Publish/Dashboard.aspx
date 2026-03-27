<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/TPM.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="FSM.Dashboard" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <!-- Bootstrap CSS CDN -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous">

    <style>
        :root {
            --shadow-lg: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -2px rgba(0, 0, 0, 0.1);
            --text-gray-700: #4b5563;
            --text-blue-700: #1e40af;
            --bg-light: #f3f4f6;
            --bg-dark: #1f2937;
        }

        [data-theme="dark"] {
            --shadow-lg: 0 4px 6px -1px rgba(0, 0, 0, 0.3), 0 2px 4px -2px rgba(0, 0, 0, 0.2);
            --text-gray-700: #d1d5db;
            --text-blue-700: #60a5fa;
            --bg-light: #374151;
            --bg-dark: #111827;
        }

        body {
            background-color: var(--bg-light);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
        }

        .container-fluid-mockup {
            max-width: 1400px;
            margin: 0 auto;
            padding: 1rem;
            width: 100%;
        }

        .hero-section-mockup {
            position: relative;
            border-radius: 8px;
            padding: 2rem;
            margin-top: 5rem;
            box-shadow: var(--shadow-lg);
            display: flex;
            flex-direction: column;
            justify-content: flex-start;
            align-items: center;
            height: 600px;
            overflow: hidden;
        }

        [data-theme="dark"] .hero-section-mockup {
            background: linear-gradient(135deg, rgba(255, 255, 255, 0.1) 0%, rgba(255, 255, 255, 0.05) 100%);
            backdrop-filter: blur(8px);
            -webkit-backdrop-filter: blur(8px);
        }

        .hero-image-mockup {
            max-width: 100%;
            max-height: 80%; /* Prevent image from taking full height on mobile */
            border-radius: 8px;
            position: absolute;
            z-index: 1;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -40%); /* Center the image */
        }

        .hero-text-mockup {
            position: relative;
            z-index: 2;
            text-align: center;
            margin-bottom: 1rem;
            transform: translate(40%, 50%);
        }

        [data-theme="dark"] .page-title-mockup {
            color: rgb(94 124 253);
        }

        .page-title-mockup {
            font-size: clamp(2rem, 5vw, 3rem);
            font-weight: 700;
            color: #0d6efd;
            margin-bottom: -0.5rem;
        }

        [data-theme="dark"] .subtitle-mockup {
            color: white;
        }

        .subtitle-mockup {
            font-size: 40px;
            font-weight: 400;
            color: #305072;
            margin-bottom: 1rem;
        }

        /* Responsive Design */
        @media (max-width: 992px) {
            .hero-section-mockup {
                height: 400px;
                padding: 1.5rem;
                margin: 3rem auto; /* Reduce margin for medium screens */
            }

            .hero-text-mockup {
                transform: translate(0%, 0%);
            }

            .page-title-mockup {
                font-size: clamp(1.75rem, 4vw, 2.5rem);
            }

            .subtitle-mockup {
                font-size: clamp(1rem, 2.5vw, 1.5rem);
            }

            .hero-image-mockup {
                max-height: 70%; /* Slightly smaller on medium screens */
            }
        }

        @media (max-width: 576px) {
            .container-fluid-mockup {
                padding: 0.5rem;
            }

            .hero-section-mockup {
                margin: 1rem auto;
                padding: 1rem;
                height: 300px; /* Reduced height for mobile */
            }

            .page-title-mockup {
                font-size: clamp(1.5rem, 3.5vw, 2rem); /* Smaller font for mobile */
                margin-bottom: 0.5rem;
            }

            .subtitle-mockup {
                font-size: clamp(0.875rem, 2vw, 1.25rem); /* Smaller font for mobile */
            }

            .hero-image-mockup {
                max-height: 80%; /* Smaller image on mobile */
                max-width: 90%; /* Ensure image fits within mobile width */
            }
        }
    </style>

    <main class="main-content-mockup container-fluid container-fluid-mockup">
        <!-- Hero Section -->
         <h1 class="page-title-mockup">Welcome to TPM</h1>
                <p class="subtitle-mockup">3rd Party Module </p>
        <section class="hero-section-mockup">
            <div class="hero-text-mockup">
               
            </div>
           
              <%--<img src="https://central.xceleran.com/fsm/images/fsmmain.png" alt="Technician with Van" class="hero-image-mockup">--%>
        </section>
         <br />
            <img src="images/TPM_IMG.png" alt="Technician with Van" class="hero-image-mockup">
    </main>

    <!-- Bootstrap JS CDN -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js" integrity="sha384-YvpcrYf0tY3lHB60NNkmXc5s9fDVZLESaAA55NDzOxhy9GkcIdslK1eN7N6jIeHz" crossorigin="anonymous"></script>
</asp:Content>
