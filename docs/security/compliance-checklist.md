# Synaxis Platform Compliance Checklist

**Document Date:** March 4, 2026  
**Version:** 1.0  
**Status:** In Review  

---

## ISO 27001:2022 Compliance

### A.5 Organizational Controls

| Control | Requirement | Status | Evidence | Gap |
|---------|-------------|--------|----------|-----|
| A.5.1 | Information security policies | Partial | Security policies documented | Need formal approval process |
| A.5.2 | Information security roles | Compliant | RBAC implemented | - |
| A.5.3 | Segregation of duties | Partial | Teams separation exists | Need formal SOD matrix |
| A.5.4 | Management responsibilities | Partial | Roles defined | Need security committee |

### A.6 People Controls

| Control | Requirement | Status | Evidence | Gap |
|---------|-------------|--------|----------|-----|
| A.6.1 | Screening | Not Started | - | Implement background checks |
| A.6.2 | Terms of employment | Partial | Code of conduct exists | Update for security |
| A.6.3 | Awareness education | Partial | Documentation exists | Regular training needed |
| A.6.4 | Disciplinary process | Not Started | - | Define security violation process |

### A.7 Physical Controls

| Control | Requirement | Status | Evidence | Gap |
|---------|-------------|--------|----------|-----|
| A.7.1 | Physical security perimeters | Compliant | AWS VPC security | - |
| A.7.2 | Physical entry controls | Partial | Security groups | Need bastion host access logs |
| A.7.3 | Securing offices | N/A | Cloud-based | - |
| A.7.4 | Physical security monitoring | Partial | CloudWatch logs | Need SIEM integration |

### A.8 Technological Controls

#### A.8.1 User Endpoint Devices

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.8.1.1 | Endpoint security | Partial | MDM not implemented |
| A.8.1.2 | Privileged access | Compliant | SuperAdmin middleware |
| A.8.1.3 | Restriction of software | Partial | Need endpoint policies |

#### A.8.2 Privileged Access Rights

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.8.2.1 | Assignment of privileges | Compliant | Authorization policies |
| A.8.2.2 | Privileged access management | Partial | SuperAdmin logging | Need PAM tool |
| A.8.2.3 | Privileged access restrictions | Compliant | 15-min timeout |

#### A.8.3 Information Access Restriction

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.8.3.1 | Information access restriction | Compliant | RBAC implementation |
| A.8.3.2 | Secure log-on procedures | Compliant | JWT + MFA |
| A.8.3.3 | Password management | Partial | Need complexity rules |

#### A.8.4 Security of Source Code

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.8.4.1 | Source code access | Partial | GitHub access controls |
| A.8.4.2 | Source code protection | Compliant | Secrets scanning |

### A.9 Access Controls

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.9.1.1 | Access to networks | Compliant | Network policies |
| A.9.1.2 | Access to operating systems | Compliant | Pod security standards |
| A.9.2.1 | User registration | Compliant | User service |
| A.9.2.2 | Privilege management | Compliant | RBAC handlers |
| A.9.2.3 | Removal of access | Compliant | User deletion flow |
| A.9.2.4 | Review of access rights | Partial | Need quarterly review |
| A.9.2.5 | Secure authentication | Compliant | JWT + MFA |
| A.9.2.6 | Password management | Partial | Need complexity |
| A.9.3.1 | Use of secret authentication | Compliant | API key validation |
| A.9.4.1 | Information access restriction | Compliant | Organization isolation |

### A.10 Cryptography

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.10.1.1 | Cryptographic controls | Compliant | TLS 1.3, AES-256 |
| A.10.1.2 | Key management | Partial | AWS KMS | Need rotation |

### A.11 Physical and Environmental

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.11.1.1 | Equipment location | N/A | Cloud provider |
| A.11.1.2 | Physical entry controls | Compliant | AWS security groups |

### A.12 Operations Security

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.12.1.1 | Operating procedures | Partial | Runbooks exist |
| A.12.1.2 | Change management | Partial | GitOps workflow |
| A.12.2.1 | Malware protection | Partial | Container scanning |
| A.12.3.1 | Information backup | Compliant | Automated backups |
| A.12.4.1 | Event logging | Compliant | AuditLog model |
| A.12.4.2 | Protection of log information | Compliant | Tamper-evident |
| A.12.4.3 | Administrator and operator logs | Compliant | SuperAdmin logging |
| A.12.4.4 | Clock synchronization | Partial | Need NTP |
| A.12.5.1 | Installation of software | Partial | Container images |
| A.12.6.1 | Technical vulnerability management | Partial | Need scanning |
| A.12.6.2 | Restrictions on software installation | Partial | Image scanning |
| A.12.7.1 | Controls against malware | Partial | Container scanning |

### A.13 Communications Security

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.13.1.1 | Network controls | Compliant | Network policies |
| A.13.1.2 | Network services security | Compliant | Security groups |
| A.13.1.3 | Segregation in networks | Compliant | VPC subnets |
| A.13.2.1 | Information transfer policies | Partial | Need data classification |
| A.13.2.2 | Agreements on information transfer | N/A | Internal only |
| A.13.2.3 | Electronic messaging | Partial | Email service |
| A.13.2.4 | Confidentiality agreements | N/A | Internal |

### A.14 System Acquisition

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.14.1.1 | Information security requirements | Partial | Security requirements |
| A.14.1.2 | Securing application services | Partial | Secure coding |
| A.14.1.3 | Protecting application services | Compliant | WAF, security headers |
| A.14.2.1 | Secure development policy | Partial | Need formal policy |
| A.14.2.2 | System change control procedures | Compliant | GitOps |
| A.14.2.3 | Technical review of applications | Partial | Code review |
| A.14.2.4 | Restrictions on changes to software | Compliant | Git workflow |
| A.14.2.5 | Secure system engineering principles | Partial | Architecture documented |
| A.14.2.6 | Secure development environment | Partial | Dev environment |
| A.14.2.7 | Outsourced development | N/A | In-house |
| A.14.2.8 | System security testing | Partial | Unit tests |
| A.14.2.9 | Acceptance testing | Partial | Integration tests |

### A.15 Supplier Relationships

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.15.1.1 | Information security policy for supplier | Partial | AWS compliance |
| A.15.1.2 | Addressing security within agreements | Partial | AWS agreements |
| A.15.1.3 | Information and communication technology supply chain | Partial | Dependency scanning |
| A.15.2.1 | Monitoring and review of supplier services | Partial | AWS monitoring |
| A.15.2.2 | Managing changes to supplier services | Partial | Change management |

### A.16 Incident Management

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.16.1.1 | Responsibilities and procedures | Partial | Need formal IR plan |
| A.16.1.2 | Reporting information security events | Partial | Logging |
| A.16.1.3 | Reporting information security weaknesses | Partial | Vulnerability process |
| A.16.1.4 | Assessment of and decision on information security events | Partial | Need formal process |
| A.16.1.5 | Response to information security incidents | Partial | Need formal process |
| A.16.1.6 | Learning from information security incidents | Not Started | - |
| A.16.1.7 | Collection of evidence | Partial | Audit logs |

### A.17 Business Continuity

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.17.1.1 | Planning information security continuity | Partial | Multi-AZ deployment |
| A.17.1.2 | Implementing information security continuity | Partial | Backup procedures |
| A.17.1.3 | Verify, review, and evaluate | Partial | DR testing |
| A.17.2.1 | Availability of information processing facilities | Compliant | Multi-AZ RDS |

### A.18 Compliance

| Control | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| A.18.1.1 | Identification of applicable legislation | Partial | GDPR, CCPA awareness |
| A.18.1.2 | Intellectual property rights | Compliant | License headers |
| A.18.1.3 | Protection of records | Compliant | Audit logging |
| A.18.1.4 | Privacy and protection of PII | Partial | PII in audit logs |
| A.18.1.5 | Regulation of cryptographic controls | Compliant | Standard algorithms |
| A.18.2.1 | Independent review | Partial | Code review |
| A.18.2.2 | Compliance with security policies | Partial | Policy documentation |
| A.18.2.3 | Technical compliance checking | Partial | Need automation |

---

## SOC 2 Type II Trust Service Criteria

### Security

| Criteria | Principle | Status | Evidence |
|----------|-----------|--------|----------|
| CC6.1 | Logical access security | Compliant | RBAC, MFA |
| CC6.2 | Access removal | Compliant | User deletion |
| CC6.3 | Access changes | Compliant | Role management |
| CC6.4 | Authentication | Compliant | JWT + MFA |
| CC6.5 | Authentication strength | Partial | Need password complexity |
| CC6.6 | Logical access events | Compliant | Audit logging |
| CC6.7 | Access to system components | Compliant | Network policies |
| CC6.8 | Security infrastructure | Compliant | WAF, security groups |
| CC7.1 | Security detection | Partial | Need SIEM |
| CC7.2 | Incident monitoring | Partial | Need formal process |
| CC7.3 | Incident response | Partial | Need formal plan |
| CC7.4 | Incident mitigation | Partial | Need formal process |
| CC7.5 | Incident recovery | Partial | Need DR plan |

### Availability

| Criteria | Principle | Status | Evidence |
|----------|-----------|--------|----------|
| A1.1 | Availability commitments | Compliant | SLA documentation |
| A1.2 | System availability | Compliant | Multi-AZ deployment |
| A1.3 | Recovery point objective | Partial | Need formal RPO |
| A1.4 | Recovery time objective | Partial | Need formal RTO |

### Confidentiality

| Criteria | Principle | Status | Evidence |
|----------|-----------|--------|----------|
| C1.1 | Confidentiality commitments | Partial | Privacy policy |
| C1.2 | Confidentiality agreements | Partial | Employee agreements |
| C1.3 | Confidentiality procedures | Partial | Data handling |
| C1.4 | Confidentiality protection | Partial | Encryption |

### Processing Integrity

| Criteria | Principle | Status | Evidence |
|----------|-----------|--------|----------|
| PI1.1 | Processing integrity | Compliant | Transaction logging |
| PI1.2 | Input processing | Compliant | Validation |
| PI1.3 | Data processing | Compliant | Audit logs |
| PI1.4 | Output processing | Compliant | Integrity hashes |

### Privacy

| Criteria | Principle | Status | Evidence |
|----------|-----------|--------|----------|
| P1.1 | Notice | Partial | Privacy policy |
| P1.2 | Choice and consent | Partial | Consent mechanism |
| P1.3 | Collection | Partial | Data minimization |
| P1.4 | Use and disclosure | Partial | Data handling |
| P1.5 | Access | Partial | User data access |
| P1.6 | Data integrity | Compliant | Validation |
| P1.7 | Data security | Partial | Encryption |
| P1.8 | Data disposal | Partial | Deletion process |

---

## GDPR Compliance

| Article | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| 5 | Principles | Partial | Data minimization |
| 6 | Lawfulness | Partial | Consent mechanism |
| 7 | Conditions for consent | Partial | Need consent tracking |
| 12-14 | Transparency | Partial | Privacy notice |
| 15 | Right of access | Partial | Data export |
| 16 | Right to rectification | Compliant | User profile update |
| 17 | Right to erasure | Partial | Need full deletion |
| 18 | Right to restriction | Not Started | - |
| 20 | Right to data portability | Partial | Need export format |
| 21 | Right to object | Not Started | - |
| 25 | Data protection by design | Partial | Security by default |
| 30 | Records of processing | Not Started | - |
| 32 | Security of processing | Partial | Encryption, access |
| 33 | Breach notification | Partial | Need formal process |
| 34 | Communication to data subject | Not Started | - |
| 35 | DPIA | Not Started | - |

---

## CCPA Compliance

| Section | Requirement | Status | Evidence |
|---------|-------------|--------|----------|
| 1798.100 | Notice at collection | Partial | Privacy policy |
| 1798.105 | Right to know | Partial | Data inventory |
| 1798.106 | Right to delete | Partial | Deletion process |
| 1798.120 | Right to opt-out | Compliant | No data sale |
| 1798.125 | Non-discrimination | Compliant | No discrimination |
| 1798.130 | Disclosure methods | Partial | Privacy policy |
| 1798.140 | Definitions | N/A | - |
| 1798.145 | Exemptions | N/A | - |
| 1798.150 | Civil penalties | N/A | - |
| 1798.155 | Regulations | N/A | - |
| 1798.185 | Definitions | N/A | - |
| 1798.196 | Service providers | Partial | AWS agreement |

---

## Compliance Summary

### Overall Compliance Score: 67%

| Framework | Score | Status |
|-----------|-------|--------|
| ISO 27001:2022 | 72% | Partial Compliance |
| SOC 2 Type II | 65% | Partial Compliance |
| GDPR | 58% | Partial Compliance |
| CCPA | 70% | Partial Compliance |

### Critical Gaps

1. **ISO 27001**
   - A.6.1: Screening not implemented
   - A.16.1: Incident management procedures incomplete
   - A.18.1: Privacy protection gaps

2. **SOC 2**
   - CC7.1-CC7.5: Incident response incomplete
   - A1.3-A1.4: No formal RTO/RPO

3. **GDPR**
   - Article 17: Right to erasure incomplete
   - Article 30: No processing records
   - Article 33: Breach notification process incomplete

4. **CCPA**
   - Section 1798.105: Right to know needs enhancement
   - Section 1798.106: Right to delete incomplete

---

## Remediation Priorities

### P0 - Critical (14 days)
- Implement incident response procedures (ISO A.16.1)
- Complete GDPR Article 17 (right to erasure)
- Establish RTO/RPO for SOC 2

### P1 - High (30 days)
- Implement screening procedures (A.6.1)
- Complete breach notification process (GDPR 33)
- Implement processing records (GDPR 30)
- Complete incident response for SOC 2

### P2 - Medium (60 days)
- Formalize DR procedures
- Complete DPIA (GDPR 35)
- Implement privacy by design (GDPR 25)
- Complete CCPA disclosure methods

### P3 - Low (90 days)
- Employee security training program
- Regular compliance reviews
- Third-party audit

---

*Document Version: 1.0*  
*Last Updated: March 4, 2026*  
*Next Review: June 4, 2026*
